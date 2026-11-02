import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { errorInterceptor } from '../../core/api/error.interceptor';
import { ToastService } from '../../shared/ui/toast/toast.service';
import { ReportDialogComponent } from './report-dialog.component';

const BURSARY_ID = '0196a5f0-0000-7000-8000-000000000101';
const REPORTS_URL = `/api/v1/bursaries/${BURSARY_ID}/reports`;

describe('ReportDialogComponent', () => {
  let fixture: ComponentFixture<ReportDialogComponent>;
  let el: HTMLElement;
  let close: ReturnType<typeof vi.fn>;
  let http: HttpTestingController;

  beforeEach(() => {
    close = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withFetch(), withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: DIALOG_DATA, useValue: { bursaryId: BURSARY_ID } },
        { provide: DialogRef, useValue: { close } },
      ],
    });
    fixture = TestBed.createComponent(ReportDialogComponent);
    fixture.detectChanges();
    el = fixture.nativeElement as HTMLElement;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function setValue(selector: string, value: string) {
    const input = el.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function chooseReason(label: string) {
    const option = Array.from(el.querySelectorAll('label')).find((l) => l.textContent!.trim() === label)!;
    option.querySelector<HTMLInputElement>('input')!.click();
    fixture.detectChanges();
  }

  function send() {
    el.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();
  }

  it('send_noReason_showsReasonErrorAndSendsNothing', () => {
    send();
    expect(el.querySelector('#reason-error')!.textContent).toContain('Choose a reason.');
    http.expectNone(REPORTS_URL);
  });

  it('send_commentOverMaxLength_showsCommentErrorAndSendsNothing', () => {
    chooseReason('Broken link');
    setValue('#report-comment', 'x'.repeat(1001));
    send();
    expect(el.querySelector('#comment-error')!.textContent).toContain('1000 characters or fewer');
    expect(el.querySelector('#report-comment')!.getAttribute('aria-invalid')).toBe('true');
    http.expectNone(REPORTS_URL);
  });

  it('comment_typed_showsLiveCounter', () => {
    setValue('#report-comment', 'Closed in April');
    expect(el.querySelector('#comment-count')!.textContent).toContain('15/1000');
  });

  it('send_invalidEmail_showsEmailErrorAndSendsNothing', () => {
    chooseReason('Something else');
    setValue('#report-email', 'not-an-email');
    send();
    expect(el.querySelector('#email-error')!.textContent).toContain('name@example.co.za');
    http.expectNone(REPORTS_URL);
  });

  it('send_201_postsReportClosesDialogAndShowsToast', () => {
    chooseReason('Broken link');
    setValue('#report-comment', 'The link 404s');
    send();
    const req = http.expectOne(REPORTS_URL);
    expect(req.request.body).toEqual({ reason: 'BrokenLink', comment: 'The link 404s' });
    req.flush({ id: 'report-1' }, { status: 201, statusText: 'Created' });
    expect(close).toHaveBeenCalledTimes(1);
    expect(TestBed.inject(ToastService).message()).toBe('Report sent');
  });

  it('send_400_showsFieldErrorsFromTheApi', () => {
    chooseReason('Something else');
    setValue('#report-email', 'student@example.co.za');
    send();
    // The API's model-validation shape: messages keyed by field name.
    http.expectOne(REPORTS_URL).flush(
      { title: 'One or more validation errors occurred.', errors: { contactEmail: ['Enter a valid email.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();
    expect(el.querySelector('#email-error')!.textContent).toContain('Enter a valid email.');
    expect(close).not.toHaveBeenCalled();
  });

  it('send_429_showsRateLimitMessageInline', () => {
    chooseReason('Something else');
    send();
    http.expectOne(REPORTS_URL).flush({ title: 'Too many requests' }, { status: 429, statusText: 'Too Many Requests' });
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')!.textContent).toContain(
      'Too many reports from your connection. Please try again later.',
    );
    expect(close).not.toHaveBeenCalled();
  });

  it('send_serverError_showsGenericMessageAndKeepsSubmitEnabledForRetry', () => {
    chooseReason('Something else');
    send();
    http.expectOne(REPORTS_URL).flush({ title: 'Boom' }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')!.textContent).toContain('Check your connection and try again.');
    expect(el.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(false);
  });

  it('send_inFlight_disablesSubmitButton', () => {
    chooseReason('Something else');
    send();
    expect(el.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(true);
    http.expectOne(REPORTS_URL).flush({ id: 'report-1' }, { status: 201, statusText: 'Created' });
  });
});
