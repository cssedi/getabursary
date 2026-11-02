import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { closedBursary, openBursary } from '../../../testing/fixtures';
import { TrackerEntryResponse } from '../../core/api/models';
import { errorInterceptor } from '../../core/api/error.interceptor';
import { AuthService } from '../../core/auth/auth.service';
import { TODAY } from '../../shared/format/today';
import { NOTES_DEBOUNCE_MS } from './tracker-row.component';
import { TrackerPage, statusCounts } from './tracker.page';

const entry = (over: Partial<TrackerEntryResponse> = {}): TrackerEntryResponse => ({
  bursary: openBursary,
  status: 'Saved',
  appliedOn: null,
  notes: null,
  updatedAt: '2026-11-02T08:00:00Z',
  ...over,
});

/** Lets the store's promise chain and the signal updates it triggers finish. */
const settle = async (f: { whenStable(): Promise<unknown> }) => {
  await f.whenStable();
  await new Promise((r) => setTimeout(r));
};

describe('TrackerPage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: TODAY, useValue: '2026-11-02' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const loading = TestBed.inject(AuthService).load();
    http.expectOne('/api/v1/me').flush({ id: 'u-1', email: 'a@b.co.za', roles: [] });
    await loading;
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  async function render(entries: TrackerEntryResponse[]) {
    const fixture = TestBed.createComponent(TrackerPage);
    fixture.detectChanges();
    // The page and the store each fetch the list.
    const reqs = http.match('/api/v1/me/tracker');
    expect(reqs.length).toBe(2);
    reqs.forEach((r) => r.flush(entries));
    await settle(fixture);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('statusCounts_mixed_listsNonEmptyStatusesInOrder', () => {
    const list = [entry(), entry(), entry(), entry({ status: 'Applied' }), entry({ status: 'Awarded' })];
    expect(statusCounts(list)).toBe('3 saved, 1 applied, 1 awarded');
  });

  it('rows_listedWithCounts_andClosedHasNoLink', async () => {
    const { el } = await render([entry(), entry({ bursary: closedBursary, status: 'Applied' })]);
    expect(el.querySelector('.counts')!.textContent).toBe('1 saved, 1 applied');
    expect(el.querySelectorAll('app-tracker-row').length).toBe(2);
    expect(el.querySelectorAll('h2 a').length).toBe(1);
    expect(el.textContent).toContain('Closed');
  });

  it('statusChange_putsAllFields_andRollsBackOnError', async () => {
    const { fixture, el } = await render([entry({ notes: 'ID copy' })]);
    const select = el.querySelector<HTMLSelectElement>('select.status')!;
    select.value = 'Applied';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(el.querySelector('.counts')!.textContent).toBe('1 applied');
    const put = http.expectOne('/api/v1/me/tracker/' + openBursary.id);
    expect(put.request.body).toEqual({ status: 'Applied', appliedOn: null, notes: 'ID copy' });
    put.flush({}, { status: 500, statusText: 'Server Error' });
    await settle(fixture);
    fixture.detectChanges();
    expect(el.querySelector('.counts')!.textContent).toBe('1 saved');
  });

  it('statusFailure_doesNotUndoLaterDateEdit', async () => {
    const { fixture, el } = await render([entry()]);
    const select = el.querySelector<HTMLSelectElement>('select.status')!;
    select.value = 'Applied';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    const date = el.querySelector<HTMLInputElement>('input.applied-on')!;
    date.value = '2026-11-01';
    date.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    const puts = http.match('/api/v1/me/tracker/' + openBursary.id);
    expect(puts.length).toBe(2);
    puts[0].flush({}, { status: 500, statusText: 'Server Error' });
    puts[1].flush(entry());
    await settle(fixture);
    fixture.detectChanges();
    expect(el.querySelector('.counts')!.textContent).toBe('1 saved');
    expect(el.querySelector<HTMLInputElement>('input.applied-on')!.value).toBe('2026-11-01');
  });

  it('notes_blurWithPending_savesImmediately', async () => {
    const { el } = await render([entry()]);
    const area = el.querySelector<HTMLTextAreaElement>('textarea.notes')!;
    area.value = 'Call now';
    area.dispatchEvent(new Event('input'));
    area.dispatchEvent(new Event('blur'));
    const put = http.expectOne('/api/v1/me/tracker/' + openBursary.id);
    expect(put.request.body.notes).toBe('Call now');
    put.flush(entry());
  });

  it('open_afterListChangedElsewhere_showsFreshList', async () => {
    const first = await render([]);
    first.fixture.destroy();
    const fixture = TestBed.createComponent(TrackerPage);
    fixture.detectChanges();
    http.expectOne('/api/v1/me/tracker').flush([entry()]);
    await settle(fixture);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('app-tracker-row').length).toBe(1);
  });

  it('notes_typed_savesOnceAfterDebounce', async () => {
    const { fixture, el } = await render([entry()]);
    vi.useFakeTimers();
    const area = el.querySelector<HTMLTextAreaElement>('textarea.notes')!;
    area.value = 'Call';
    area.dispatchEvent(new Event('input'));
    area.value = 'Call the bursary office';
    area.dispatchEvent(new Event('input'));
    http.expectNone('/api/v1/me/tracker/' + openBursary.id);
    vi.advanceTimersByTime(NOTES_DEBOUNCE_MS);
    const put = http.expectOne('/api/v1/me/tracker/' + openBursary.id);
    expect(put.request.body.notes).toBe('Call the bursary office');
    put.flush(entry());
    fixture.detectChanges();
  });

  it('remove_deletesAndShowsEmptyState', async () => {
    const { fixture, el } = await render([entry()]);
    el.querySelector<HTMLButtonElement>('button.remove')!.click();
    const del = http.expectOne('/api/v1/me/tracker/' + openBursary.id);
    expect(del.request.method).toBe('DELETE');
    del.flush(null, { status: 204, statusText: 'No Content' });
    await settle(fixture);
    fixture.detectChanges();
    expect(el.querySelector('.empty')!.textContent).toContain('Save bursaries to track them here.');
    expect(el.querySelector('.empty a')!.getAttribute('href')).toBe('/search');
  });

  it('empty_showsInviteAndSearchLink', async () => {
    const { el } = await render([]);
    expect(el.querySelector('.empty')!.textContent).toContain('Save bursaries to track them here.');
  });
});
