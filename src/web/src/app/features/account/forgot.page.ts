import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AuthApi } from '../../core/api/auth-api';
import { authErrors } from '../../core/auth/auth-errors';

/** `/account/forgot`: asks Identity to email a reset link. It answers 200 whether or not the email has an account. */
@Component({
  selector: 'app-forgot-page',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="mb-4 text-2xl font-bold">Reset your password</h1>
    @if (sent()) {
      <p role="status">If that email has an account, we’ve sent a link to reset your password.</p>
    } @else {
      <form class="max-w-sm" (ngSubmit)="send()" novalidate>
        <label for="forgot-email" class="mb-1 block font-medium">Email</label>
        <input
          id="forgot-email"
          type="email"
          name="email"
          class="mb-1 min-h-11 w-full rounded-control border border-line px-3"
          placeholder="name@example.co.za…"
          autocomplete="email"
          spellcheck="false"
          inputmode="email"
          [formControl]="email"
          [attr.aria-invalid]="error() ? 'true' : null"
          [attr.aria-describedby]="error() ? 'forgot-error' : null"
        />
        @if (error(); as text) {
          <p id="forgot-error" class="mb-3 text-sm font-medium">{{ text }}</p>
        }
        <button
          type="submit"
          class="mt-3 min-h-11 rounded-control bg-jacaranda px-4 font-semibold text-paper hover:bg-ink active:bg-ink disabled:opacity-60"
          [disabled]="sending()"
        >
          {{ sending() ? 'Sending…' : 'Send reset link' }}
        </button>
      </form>
    }
  `,
})
export class ForgotPage {
  private readonly api = inject(AuthApi);
  protected readonly email = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.email],
  });
  protected readonly sending = signal(false);
  protected readonly sent = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async send(): Promise<void> {
    if (this.email.invalid) {
      this.error.set('Enter an email address like name@example.co.za.');
      return;
    }
    this.error.set(null);
    this.sending.set(true);
    try {
      await firstValueFrom(this.api.forgotPassword(this.email.value.trim()));
      this.sent.set(true);
    } catch (e) {
      this.error.set(authErrors(e).form ?? null);
    } finally {
      this.sending.set(false);
    }
  }
}
