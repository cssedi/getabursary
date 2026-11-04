import { DialogRef } from '@angular/cdk/dialog';
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
import { Router, RouterLink } from '@angular/router';
import { AuthErrors, authErrors } from '../../core/auth/auth-errors';
import { AuthService } from '../../core/auth/auth.service';

type Mode = 'signIn' | 'signUp';

const MIN_PASSWORD = 8;
const CLIENT_ERRORS = {
  email: 'Enter an email address like name@example.co.za.',
  password: 'Enter your password.',
  newPassword: `Use at least ${MIN_PASSWORD} characters for your password.`,
};

/** Sign in / create account (FR-09). Opened by `AuthService`; closes itself after a successful sign-in. */
@Component({
  selector: 'app-sign-in-dialog',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  // None: the dialog panel lives outside this component's view, so its class is styled here.
  encapsulation: ViewEncapsulation.None,
  template: `
    <div class="mb-4 flex gap-2" role="tablist" aria-label="Account">
      <button
        type="button"
        role="tab"
        class="min-h-11 rounded-control px-4 font-semibold hover:bg-bloom active:bg-bloom"
        [class.bg-bloom]="mode() === 'signIn'"
        [attr.aria-selected]="mode() === 'signIn'"
        (click)="select('signIn')"
      >
        Sign in
      </button>
      <button
        type="button"
        role="tab"
        class="min-h-11 rounded-control px-4 font-semibold hover:bg-bloom active:bg-bloom"
        [class.bg-bloom]="mode() === 'signUp'"
        [attr.aria-selected]="mode() === 'signUp'"
        (click)="select('signUp')"
      >
        Create account
      </button>
    </div>

    @if (created()) {
      <p role="status" class="mb-6 font-medium">Check your email to confirm your account.</p>
      <div class="flex justify-end">
        <button
          type="button"
          class="min-h-11 rounded-control border border-line px-4 font-medium hover:bg-bloom active:bg-bloom"
          (click)="ref.close()"
        >
          Close
        </button>
      </div>
    } @else {
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <div class="mb-4">
          <label for="auth-email" class="mb-1 block font-medium">Email</label>
          <input
            id="auth-email"
            type="email"
            name="email"
            class="min-h-11 w-full rounded-control border border-line px-3"
            placeholder="name@example.co.za…"
            autocomplete="email"
            spellcheck="false"
            inputmode="email"
            formControlName="email"
            [attr.aria-invalid]="message('email') ? 'true' : null"
            [attr.aria-describedby]="message('email') ? 'auth-email-error' : null"
          />
          @if (message('email'); as text) {
            <p id="auth-email-error" class="mt-1 text-sm font-medium">{{ text }}</p>
          }
        </div>

        <div class="mb-4">
          <label for="auth-password" class="mb-1 block font-medium">Password</label>
          <input
            id="auth-password"
            type="password"
            name="password"
            class="min-h-11 w-full rounded-control border border-line px-3"
            [attr.autocomplete]="mode() === 'signIn' ? 'current-password' : 'new-password'"
            formControlName="password"
            [attr.aria-invalid]="message('password') ? 'true' : null"
            [attr.aria-describedby]="message('password') ? 'auth-password-error' : null"
          />
          @if (message('password'); as text) {
            <p id="auth-password-error" class="mt-1 text-sm font-medium">{{ text }}</p>
          }
        </div>

        @if (mode() === 'signIn') {
          <p class="mb-4">
            <a routerLink="/account/forgot" class="text-jacaranda underline hover:text-ink" (click)="ref.close()"
              >Forgot password?</a
            >
          </p>
        }

        @if (failure(); as text) {
          <p role="alert" class="mb-4 font-medium">{{ text }}</p>
        }

        <button
          type="submit"
          class="btn-primary mb-3 w-full"
          [disabled]="sending()"
        >
          @if (sending()) {
            {{ mode() === 'signIn' ? 'Signing in…' : 'Creating account…' }}
          } @else {
            {{ mode() === 'signIn' ? 'Sign in' : 'Create account' }}
          }
        </button>
        <a
          [href]="googleUrl"
          class="btn-secondary w-full"
          >Continue with Google</a
        >
      </form>
    }
  `,
  styles: `
    .auth-dialog-backdrop {
      background: rgb(30 26 46 / 0.5);
    }
    .auth-dialog-panel app-sign-in-dialog {
      display: block;
      box-sizing: border-box;
      max-height: 90dvh;
      overflow-y: auto;
      overscroll-behavior: contain;
      padding: 24px;
      border-radius: 16px;
      background: var(--paper);
      box-shadow: 0 8px 32px rgb(27 26 23 / 0.16);
    }
    /* Bottom sheet below 640 px (the open call lives in AuthService, so this is done in CSS). */
    @media (max-width: 639px) {
      .cdk-global-overlay-wrapper:has(.auth-dialog-panel) {
        align-items: flex-end;
      }
      .auth-dialog-panel {
        width: 100% !important;
        max-width: 100vw !important;
      }
      .auth-dialog-panel app-sign-in-dialog {
        padding: 24px 16px calc(16px + env(safe-area-inset-bottom));
        border-radius: 16px 16px 0 0;
      }
    }
  `,
})
export class SignInDialogComponent {
  protected readonly ref = inject(DialogRef);
  private readonly auth = inject(AuthService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly mode = signal<Mode>('signIn');
  protected readonly submitted = signal(false);
  protected readonly sending = signal(false);
  protected readonly created = signal(false);
  protected readonly serverErrors = signal<AuthErrors>({});
  protected readonly failure = signal<string | null>(null);
  protected readonly googleUrl = `/api/v1/auth/external/google?returnUrl=${encodeURIComponent(
    inject(Router).url.split('#')[0],
  )}`;

  protected readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });

  protected select(mode: Mode): void {
    this.mode.set(mode);
    this.submitted.set(false);
    this.serverErrors.set({});
    this.failure.set(null);
  }

  /** The API's message wins; otherwise the client message, once the user has tried to submit. */
  protected message(field: 'email' | 'password'): string | null {
    const fromServer = this.serverErrors()[field];
    if (fromServer) return fromServer;
    if (!this.submitted()) return null;
    const { email, password } = this.form.controls;
    if (field === 'email') return email.invalid ? CLIENT_ERRORS.email : null;
    if (password.invalid) return CLIENT_ERRORS.password;
    return this.mode() === 'signUp' && password.value.length < MIN_PASSWORD ? CLIENT_ERRORS.newPassword : null;
  }

  protected async submit(): Promise<void> {
    this.submitted.set(true);
    this.serverErrors.set({});
    this.failure.set(null);
    if (this.message('email') || this.message('password')) {
      this.focusFirstError();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.sending.set(true);
    try {
      if (this.mode() === 'signIn') {
        await this.auth.signIn(email.trim(), password);
        this.ref.close();
      } else {
        await this.auth.signUp(email.trim(), password);
        this.created.set(true);
      }
    } catch (error) {
      const errors = authErrors(error);
      this.serverErrors.set(errors);
      this.failure.set(errors.form ?? null);
      this.focusFirstError();
    } finally {
      this.sending.set(false);
    }
  }

  private focusFirstError(): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }
}
