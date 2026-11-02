import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  ViewEncapsulation,
  afterNextRender,
  inject,
  signal,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiError } from '../../core/api/api-error';
import { ReportReason } from '../../core/api/models';
import { ReportApi } from '../../core/api/report-api';
import { ToastService } from '../../shared/ui/toast/toast.service';

export interface ReportDialogData {
  bursaryId: string;
}

const COMMENT_MAX_LENGTH = 1000;

const REASONS: { value: ReportReason; label: string }[] = [
  { value: 'WrongClosingDate', label: 'Wrong closing date' },
  { value: 'WrongEligibility', label: 'Wrong eligibility' },
  { value: 'BrokenLink', label: 'Broken link' },
  { value: 'BursaryClosed', label: 'Bursary has closed' },
  { value: 'Duplicate', label: 'Duplicate listing' },
  { value: 'Other', label: 'Something else' },
];

type Field = 'reason' | 'comment' | 'contactEmail';
type FieldErrors = Partial<Record<Field, string>>;

const CLIENT_ERRORS: Record<Field, string> = {
  reason: 'Choose a reason.',
  comment: `Keep your comment to ${COMMENT_MAX_LENGTH} characters or fewer.`,
  contactEmail: 'Enter an email address like name@example.co.za.',
};
const RATE_LIMITED = 'Too many reports from your connection. Please try again later.';
const GENERIC_FAILURE = 'Your report didn’t send. Check your connection and try again.';

/** "Report a problem" (FR-08). Anyone can send one, so there is no sign-in. Opened by `ReportButtonComponent`. */
@Component({
  selector: 'app-report-dialog',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  // None: the dialog panel lives outside this component's view, so its class is styled here.
  encapsulation: ViewEncapsulation.None,
  template: `
    <h2 class="mb-4 text-xl font-bold">Report a problem</h2>
    <form [formGroup]="form" (ngSubmit)="send()">
      <fieldset class="mb-5" [attr.aria-describedby]="message('reason') ? 'reason-error' : null">
        <legend class="mb-2 font-medium">What’s wrong?</legend>
        @for (r of reasons; track r.value) {
          <label class="flex min-h-11 items-center gap-3">
            <input
              type="radio"
              class="size-5 accent-jacaranda"
              formControlName="reason"
              [value]="r.value"
              [attr.aria-invalid]="message('reason') ? 'true' : null"
            />
            {{ r.label }}
          </label>
        }
        @if (message('reason'); as text) {
          <p id="reason-error" class="mt-1 text-sm font-medium">{{ text }}</p>
        }
      </fieldset>

      <div class="mb-5">
        <label for="report-comment" class="mb-1 block font-medium">Comment (optional)</label>
        <textarea
          id="report-comment"
          name="comment"
          rows="4"
          class="w-full rounded-control border border-line p-3"
          placeholder="What did you see…"
          autocomplete="off"
          formControlName="comment"
          [attr.aria-invalid]="message('comment') ? 'true' : null"
          [attr.aria-describedby]="message('comment') ? 'comment-count comment-error' : 'comment-count'"
        ></textarea>
        <div class="mt-1 flex items-start justify-between gap-4 text-sm">
          @if (message('comment'); as text) {
            <p id="comment-error" class="font-medium">{{ text }}</p>
          }
          <p id="comment-count" class="ml-auto tabular-nums text-muted">
            {{ form.controls.comment.value.length }}/{{ commentMax }}
          </p>
        </div>
      </div>

      <div class="mb-6">
        <label for="report-email" class="mb-1 block font-medium">Email (optional)</label>
        <input
          id="report-email"
          type="email"
          name="contactEmail"
          class="min-h-11 w-full rounded-control border border-line px-3"
          placeholder="name@example.co.za…"
          autocomplete="email"
          spellcheck="false"
          inputmode="email"
          formControlName="contactEmail"
          [attr.aria-invalid]="message('contactEmail') ? 'true' : null"
          [attr.aria-describedby]="message('contactEmail') ? 'email-error' : null"
        />
        @if (message('contactEmail'); as text) {
          <p id="email-error" class="mt-1 text-sm font-medium">{{ text }}</p>
        }
      </div>

      @if (failure(); as text) {
        <p role="alert" class="mb-4 font-medium">{{ text }}</p>
      }

      <div class="flex justify-end gap-3">
        <button
          type="button"
          class="min-h-11 rounded-control border border-line px-4 font-medium hover:bg-bloom active:bg-bloom"
          (click)="ref.close()"
        >
          Cancel
        </button>
        <button
          type="submit"
          class="min-h-11 rounded-control bg-jacaranda px-4 font-semibold text-paper hover:opacity-90 active:opacity-80 disabled:opacity-60"
          [disabled]="sending()"
        >
          {{ sending() ? 'Sending…' : 'Send report' }}
        </button>
      </div>
    </form>
  `,
  styles: `
    .report-dialog-backdrop {
      background: rgb(30 26 46 / 0.5);
    }
    .report-dialog-panel app-report-dialog {
      display: block;
      box-sizing: border-box;
      max-height: 90dvh;
      overflow-y: auto;
      overscroll-behavior: contain;
      padding: 24px;
      border-radius: 16px;
      background: var(--paper);
    }
  `,
})
export class ReportDialogComponent {
  protected readonly ref = inject(DialogRef);
  private readonly data = inject<ReportDialogData>(DIALOG_DATA);
  private readonly api = inject(ReportApi);
  private readonly toast = inject(ToastService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly reasons = REASONS;
  protected readonly commentMax = COMMENT_MAX_LENGTH;
  protected readonly form = new FormGroup({
    reason: new FormControl<ReportReason | null>(null, Validators.required),
    comment: new FormControl('', {
      nonNullable: true,
      validators: Validators.maxLength(COMMENT_MAX_LENGTH),
    }),
    contactEmail: new FormControl('', { nonNullable: true, validators: Validators.email }),
  });
  protected readonly submitted = signal(false);
  protected readonly sending = signal(false);
  protected readonly serverErrors = signal<FieldErrors>({});
  protected readonly failure = signal<string | null>(null);

  /** The API's message wins; otherwise the client message, once the user has tried to send. */
  protected message(field: Field): string | null {
    const fromServer = this.serverErrors()[field];
    if (fromServer) return fromServer;
    return this.submitted() && this.form.controls[field].invalid ? CLIENT_ERRORS[field] : null;
  }

  protected send(): void {
    this.submitted.set(true);
    this.serverErrors.set({});
    this.failure.set(null);
    const reason = this.form.controls.reason.value;
    if (this.form.invalid || reason === null) {
      this.focusFirstError();
      return;
    }

    const { comment, contactEmail } = this.form.getRawValue();
    this.sending.set(true);
    this.api
      .create(this.data.bursaryId, {
        reason,
        comment: comment.trim() || undefined,
        contactEmail: contactEmail.trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.toast.show('Report sent');
          this.ref.close();
        },
        error: (error: unknown) => this.fail(error),
      });
  }

  private fail(error: unknown): void {
    this.sending.set(false);
    const apiError = error instanceof ApiError ? error : null;
    if (apiError?.status === 400) {
      const fields = fieldErrors(apiError);
      if (Object.keys(fields).length) {
        this.serverErrors.set(fields);
        this.focusFirstError();
        return;
      }
    }
    this.failure.set(apiError?.status === 429 ? RATE_LIMITED : GENERIC_FAILURE);
  }

  private focusFirstError(): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }
}

/**
 * Field messages from a 400. ASP.NET model validation keys `errors` by field name, which the error
 * interceptor leaves in `extensions`. Result failures send `{ code, message }` items in `errors`.
 */
function fieldErrors(error: ApiError): FieldErrors {
  const found: FieldErrors = {};
  const keyed = error.extensions['errors'];
  if (isRecord(keyed)) {
    for (const [name, messages] of Object.entries(keyed)) {
      const field = fieldNamed(name);
      const first = Array.isArray(messages) ? messages[0] : undefined;
      if (field && typeof first === 'string') found[field] = first;
    }
  }
  for (const item of error.errors) {
    const field = fieldNamed(item.code);
    if (field) found[field] = item.message;
  }
  return found;
}

function fieldNamed(name: string): Field | undefined {
  const lower = name.toLowerCase();
  if (lower.includes('email')) return 'contactEmail';
  if (lower.includes('comment')) return 'comment';
  if (lower.includes('reason')) return 'reason';
  return undefined;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
