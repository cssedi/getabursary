import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Provider } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { AdminBursary, ReviewItem } from '../../core/api/admin-api';
import { errorInterceptor } from '../../core/api/error.interceptor';
import { AuthService } from '../../core/auth/auth.service';
import { TODAY } from '../../shared/format/today';
import { adminGuard, unsavedChangesGuard } from './admin.guards';
import { BursaryEditorPage } from './bursary-editor.page';
import { ReviewQueuePage } from './review-queue.page';

const settle = async (f: { whenStable(): Promise<unknown> }) => {
  await f.whenStable();
  await new Promise((r) => setTimeout(r));
};

const item = (id: string, over: Partial<ReviewItem> = {}): ReviewItem => ({
  id,
  type: 'NewListing',
  status: 'Open',
  createdAt: '2026-11-01T08:00:00Z',
  bursaryId: `b-${id}`,
  bursaryName: `Bursary ${id}`,
  funderName: 'Funder',
  isPublished: false,
  bursaryStatus: 'Open',
  closingDate: '2026-12-01',
  reason: { confidence: 0.5 },
  reportReason: null,
  reportComment: null,
  ...over,
});

const bursary = (over: Partial<AdminBursary> = {}): AdminBursary => ({
  id: 'b-1',
  slug: 'b-1',
  name: 'Sasol bursary',
  funderName: 'Sasol',
  isPublished: false,
  status: 'Open',
  closingDate: '2026-12-01',
  summary: 'About',
  openDate: null,
  studyLevels: ['Undergraduate'],
  minAverage: 65,
  coverage: ['Tuition'],
  fieldCodes: [],
  applyUrl: null,
  lockedFields: [],
  ...over,
});

function setup(roles: string[], extra: Provider[] = []) {
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      provideHttpClient(withInterceptors([errorInterceptor])),
      provideHttpClientTesting(),
      { provide: TODAY, useValue: '2026-11-02' },
      ...extra,
    ],
  });
  const http = TestBed.inject(HttpTestingController);
  return {
    http,
    loadUser: async () => {
      const loading = TestBed.inject(AuthService).load();
      http.expectOne('/api/v1/me').flush({ id: 'u-1', email: 'a@b.co.za', roles });
      await loading;
    },
  };
}

describe('adminGuard', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('blocks a signed-in student so the 404 route renders', async () => {
    const { loadUser } = setup(['Student']);
    await loadUser();
    expect(await TestBed.runInInjectionContext(() => adminGuard({} as never, [], {} as never))).toBe(false);
  });

  it('blocks a signed-out visitor', async () => {
    const { http } = setup([]);
    const result = TestBed.runInInjectionContext(() => adminGuard({} as never, [], {} as never));
    http.expectOne('/api/v1/me').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(await result).toBe(false);
  });

  it('lets an admin in', async () => {
    const { loadUser } = setup(['Admin']);
    await loadUser();
    expect(await TestBed.runInInjectionContext(() => adminGuard({} as never, [], {} as never))).toBe(true);
  });
});

describe('unsavedChangesGuard', () => {
  const run = (dirty: boolean) =>
    TestBed.runInInjectionContext(() =>
      unsavedChangesGuard(
        { hasUnsavedChanges: () => dirty },
        {} as never,
        {} as never,
        {} as never,
      ),
    );

  it('lets a clean page go without asking', () => {
    const ask = vi.spyOn(window, 'confirm');
    expect(run(false)).toBe(true);
    expect(ask).not.toHaveBeenCalled();
  });

  it('asks before leaving a dirty page and honours the answer', () => {
    const ask = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true);
    expect(run(true)).toBe(false);
    expect(run(true)).toBe(true);
    expect(ask).toHaveBeenCalledTimes(2);
  });
});

describe('ReviewQueuePage', () => {
  let http: HttpTestingController;

  afterEach(() => http.verify());

  async function render(items: ReviewItem[], totalCount = items.length) {
    ({ http } = setup(['Admin']));
    const fixture = TestBed.createComponent(ReviewQueuePage);
    fixture.detectChanges();
    http
      .expectOne('/api/v1/admin/review-items')
      .flush({ items, page: 1, pageSize: 20, totalCount });
    await settle(fixture);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  const button = (el: HTMLElement, text: string, row = 0) =>
    Array.from(el.querySelectorAll('.queue-item')[row].querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === text,
    )!;

  it('lists the items with the open count', async () => {
    const { el } = await render([item('1'), item('2')]);
    expect(el.querySelectorAll('.queue-item').length).toBe(2);
    expect(el.querySelector('.open-count')?.textContent).toContain('2 open');
  });

  it('filters by type through the API', async () => {
    const { fixture, el } = await render([item('1')]);
    const select = el.querySelector<HTMLSelectElement>('#review-type')!;
    select.value = 'UserReport';
    select.dispatchEvent(new Event('change'));
    http
      .expectOne('/api/v1/admin/review-items?type=UserReport')
      .flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });
    await settle(fixture);
    fixture.detectChanges();
    expect(el.querySelector('.empty')).not.toBeNull();
  });

  it('Publish posts the action and removes the row', async () => {
    const { fixture, el } = await render([item('1'), item('2')]);
    button(el, 'Publish').click();
    fixture.detectChanges();
    expect(el.querySelectorAll('.queue-item').length).toBe(1);
    const req = http.expectOne('/api/v1/admin/review-items/1/resolve');
    expect(req.request.body).toEqual({ action: 'Publish' });
    req.flush(item('1'));
    await settle(fixture);
    expect(el.querySelector('.open-count')?.textContent).toContain('1 open');
  });

  it('Mark closed posts MarkClosed', async () => {
    const { el } = await render([item('1')]);
    button(el, 'Mark closed').click();
    const req = http.expectOne('/api/v1/admin/review-items/1/resolve');
    expect(req.request.body).toEqual({ action: 'MarkClosed' });
    req.flush(item('1'));
  });

  it('Dismiss sends the optional note', async () => {
    const { fixture, el } = await render([item('1')]);
    button(el, 'Dismiss').click();
    fixture.detectChanges();
    const input = el.querySelector<HTMLInputElement>('#note-1')!;
    input.value = 'Duplicate of another';
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    const req = http.expectOne('/api/v1/admin/review-items/1/resolve');
    expect(req.request.body).toEqual({ action: 'Dismiss', notes: 'Duplicate of another' });
    req.flush(item('1'));
  });

  it('puts the row back where it was when the API refuses', async () => {
    const { fixture, el } = await render([item('1'), item('2')]);
    button(el, 'Publish').click();
    http
      .expectOne('/api/v1/admin/review-items/1/resolve')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await settle(fixture);
    fixture.detectChanges();
    const names = Array.from(el.querySelectorAll('.queue-item')).map((r) => r.textContent);
    expect(names.length).toBe(2);
    expect(names[0]).toContain('Bursary 1');
    expect(el.querySelector('.open-count')?.textContent).toContain('2 open');
  });
});

describe('BursaryEditorPage', () => {
  let http: HttpTestingController;

  afterEach(() => http.verify());

  async function render(b: AdminBursary) {
    ({ http } = setup(
      ['Admin'],
      [{ provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', 'b-1']]) } } }],
    ));
    const fixture = TestBed.createComponent(BursaryEditorPage);
    fixture.detectChanges();
    http.expectOne('/api/v1/admin/bursaries/b-1').flush(b);
    http.expectOne('/api/v1/taxonomy/fields').flush([]);
    await settle(fixture);
    fixture.detectChanges();
    return { fixture, page: fixture.componentInstance, el: fixture.nativeElement as HTMLElement };
  }

  const type = (el: HTMLElement, selector: string, value: string) => {
    const input = el.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  };

  it('sends only the changed fields', async () => {
    const { fixture, page, el } = await render(bursary());
    expect(page.hasUnsavedChanges()).toBe(false);
    type(el, '#f-name', 'Sasol bursary 2027');
    type(el, '#f-avg', '70');
    fixture.detectChanges();
    expect(page.hasUnsavedChanges()).toBe(true);
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    const req = http.expectOne('/api/v1/admin/bursaries/b-1');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ name: 'Sasol bursary 2027', minAverage: 70 });
    req.flush(
      bursary({ name: 'Sasol bursary 2027', minAverage: 70, lockedFields: ['Name', 'MinAverage'] }),
    );
    await settle(fixture);
    expect(page.hasUnsavedChanges()).toBe(false);
  });

  it('sends a cleared date as null and a toggled level as a list', async () => {
    const { fixture, el } = await render(bursary());
    type(el, '#f-close', '');
    el.querySelector<HTMLInputElement>('[name="level-Honours"]')!.click();
    fixture.detectChanges();
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    const req = http.expectOne('/api/v1/admin/bursaries/b-1');
    expect(req.request.body).toEqual({
      closingDate: null,
      studyLevels: ['Undergraduate', 'Honours'],
    });
    req.flush(bursary());
  });

  it('blocks an invalid value with an inline message and sends nothing', async () => {
    const { fixture, el } = await render(bursary());
    type(el, '#f-avg', '150');
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    expect(el.textContent).toContain('Enter an average between 0 and 100.');
  });

  it('shows Locked badges only on locked fields and unlocks one', async () => {
    const { fixture, el } = await render(bursary({ lockedFields: ['Name', 'Eligibility'] }));
    expect(el.querySelectorAll('.locked-badge').length).toBe(2);
    el.querySelector<HTMLButtonElement>('.unlock')!.click();
    const req = http.expectOne('/api/v1/admin/bursaries/b-1/locks/Name');
    expect(req.request.method).toBe('DELETE');
    req.flush(bursary({ lockedFields: ['Eligibility'] }));
    await settle(fixture);
    fixture.detectChanges();
    expect(el.querySelectorAll('.locked-badge').length).toBe(1);
  });

  it('Publish and Unpublish patch only isPublished', async () => {
    const { fixture, el } = await render(bursary());
    const toggle = () => el.querySelector<HTMLButtonElement>('h2 + button')!;
    expect(toggle().textContent?.trim()).toBe('Publish');
    toggle().click();
    const publish = http.expectOne('/api/v1/admin/bursaries/b-1');
    expect(publish.request.body).toEqual({ isPublished: true });
    publish.flush(bursary({ isPublished: true }));
    await settle(fixture);
    fixture.detectChanges();
    expect(toggle().textContent?.trim()).toBe('Unpublish');
    toggle().click();
    const unpublish = http.expectOne('/api/v1/admin/bursaries/b-1');
    expect(unpublish.request.body).toEqual({ isPublished: false });
    unpublish.flush(bursary());
  });
});
