import { Dialog } from '@angular/cdk/dialog';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { openBursary } from '../../../../testing/fixtures';
import { errorInterceptor } from '../../../core/api/error.interceptor';
import { AuthService } from '../../../core/auth/auth.service';
import { ToastService } from '../toast/toast.service';
import { SaveButtonComponent } from './save-button.component';

@Component({
  selector: 'app-save-button-host',
  imports: [SaveButtonComponent],
  template: `<app-save-button [bursaryId]="id" name="Anglo American Engineering Bursary 2027" />`,
})
class HostComponent {
  readonly id = openBursary.id;
}

const ME = { id: 'u-1', email: 'thandi@example.co.za', roles: ['Student'] };
const ENTRY = { bursary: openBursary, status: 'Saved', appliedOn: null, notes: null, updatedAt: '2026-11-02T08:00:00Z' };

describe('SaveButtonComponent', () => {
  let http: HttpTestingController;
  let auth: AuthService;
  let open: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    sessionStorage.clear();
    open = vi.fn(() => ({ closed: { subscribe: () => undefined } }));
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: Dialog, useValue: { open } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => http.verify());

  async function render(signedIn: boolean, saved: unknown[] = []) {
    const loading = auth.load();
    const me = http.expectOne('/api/v1/me');
    if (signedIn) me.flush(ME);
    else me.flush({}, { status: 401, statusText: 'Unauthorized' });
    await loading;
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    if (signedIn) {
      http.expectOne('/api/v1/me/tracker').flush(saved);
      await fixture.whenStable();
    }
    fixture.detectChanges();
    const button = (fixture.nativeElement as HTMLElement).querySelector('button')!;
    return { fixture, button };
  }

  const tick = async (fixture: { detectChanges(): void; whenStable(): Promise<unknown> }) => {
    await fixture.whenStable();
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
  };

  it('label_unsaved_saysSaveAndName_savedSaysSaved', async () => {
    const { button } = await render(true);
    expect(button.textContent!.trim()).toBe('Save Anglo American Engineering Bursary 2027');
    TestBed.resetTestingModule();
  });

  it('click_signedOut_opensDialog_andSavesAfterSignIn', async () => {
    const { fixture, button } = await render(false);
    button.click();
    await vi.waitFor(() => expect(open).toHaveBeenCalled());
    http.expectNone('/api/v1/me/tracker/' + openBursary.id);

    const signingIn = auth.signIn(ME.email, 'secret-pass');
    http.expectOne('/api/v1/auth/login?useCookies=true').flush({});
    await fixture.whenStable();
    http.expectOne('/api/v1/me').flush(ME);
    await signingIn;
    http.expectOne('/api/v1/me/tracker').flush([]);
    await fixture.whenStable();
    const put = http.expectOne('/api/v1/me/tracker/' + openBursary.id);
    expect(put.request.method).toBe('PUT');
    expect(put.request.body).toEqual({ status: 'Saved', appliedOn: null, notes: null });
    put.flush(ENTRY, { status: 201, statusText: 'Created' });
    await tick(fixture);
    expect(button.getAttribute('aria-pressed')).toBe('true');
    expect(button.textContent!.trim()).toBe('Saved');
  });

  it('click_signedIn_savesOptimistically_andRollsBackOnError', async () => {
    const { fixture, button } = await render(true);
    button.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(button.getAttribute('aria-pressed')).toBe('true');
    http
      .expectOne('/api/v1/me/tracker/' + openBursary.id)
      .flush({ title: 'Boom' }, { status: 500, statusText: 'Server Error' });
    await tick(fixture);
    expect(button.getAttribute('aria-pressed')).toBe('false');
    expect(TestBed.inject(ToastService).message()).toContain('Couldn’t save');
  });

  it('click_saved_removes_andRollsBackOnError', async () => {
    const { fixture, button } = await render(true, [ENTRY]);
    expect(button.getAttribute('aria-pressed')).toBe('true');
    button.click();
    await tick(fixture);
    expect(button.getAttribute('aria-pressed')).toBe('false');
    const del = http.expectOne('/api/v1/me/tracker/' + openBursary.id);
    expect(del.request.method).toBe('DELETE');
    del.flush({}, { status: 500, statusText: 'Server Error' });
    await tick(fixture);
    expect(button.getAttribute('aria-pressed')).toBe('true');
  });
});
