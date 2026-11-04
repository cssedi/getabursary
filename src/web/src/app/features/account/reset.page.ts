import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthApi } from '../../core/api/auth-api';
import { authErrors } from '../../core/auth/auth-errors';
import { AuthService } from '../../core/auth/auth.service';

/** `/account/reset?email&code`: target of the reset email. */
@Component({
  selector: 'app-reset-page',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="mb-4 text-2xl font-bold">Choose a new password</h1>
    @if (done()) {
      <p role="status" class="mb-4">Password changed. Sign in with your new password.</p>
      <button
        type="button"
        class="btn-primary"
        (click)="auth.openSignIn()"
      >
        Sign in
      </button>
    } @else if (!email || !code) {
      <p role="alert">This reset link is incomplete. Ask for a new one.</p>
    } @else {
      <form class="max-w-sm" (ngSubmit)="save()" novalidate>
        <label for="reset-password" class="mb-1 block font-medium">New password</label>
        <input
          id="reset-password"
          type="password"
          name="password"
          class="mb-1 min-h-11 w-full rounded-control border border-line px-3"
          autocomplete="new-password"
          [formControl]="password"
          [attr.aria-invalid]="error() ? 'true' : null"
          [attr.aria-describedby]="error() ? 'reset-error' : null"
        />
        @if (error(); as text) {
          <p id="reset-error" class="mb-3 text-sm font-medium">{{ text }}</p>
        }
        <button
          type="submit"
          class="btn-primary mt-3"
          [disabled]="saving()"
        >
          {{ saving() ? 'Saving…' : 'Change password' }}
        </button>
      </form>
    }
  `,
})
export class ResetPage {
  protected readonly auth = inject(AuthService);
  private readonly api = inject(AuthApi);
  private readonly query = inject(ActivatedRoute).snapshot.queryParamMap;
  protected readonly email = this.query.get('email');
  protected readonly code = this.query.get('code');
  protected readonly password = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(8)],
  });
  protected readonly saving = signal(false);
  protected readonly done = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async save(): Promise<void> {
    if (this.password.invalid) {
      this.error.set('Use at least 8 characters for your password.');
      return;
    }
    this.error.set(null);
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.resetPassword(this.email!, this.code!, this.password.value));
      this.done.set(true);
    } catch (e) {
      const errors = authErrors(e);
      this.error.set(errors.password ?? errors.form ?? null);
    } finally {
      this.saving.set(false);
    }
  }
}
