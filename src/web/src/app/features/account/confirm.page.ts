import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthApi } from '../../core/api/auth-api';
import { AuthService } from '../../core/auth/auth.service';

/** Target of the link in the confirmation email: `/account/confirm?userId&code`. */
@Component({
  selector: 'app-confirm-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="mb-4 text-2xl font-bold">Confirm your email</h1>
    @switch (state()) {
      @case ('working') {
        <p role="status">Confirming your email…</p>
      }
      @case ('done') {
        <p role="status" class="mb-4">Email confirmed. You can save bursaries now.</p>
        <button
          type="button"
          class="min-h-11 rounded-control bg-jacaranda px-4 font-semibold text-paper hover:bg-ink active:bg-ink"
          (click)="auth.openSignIn()"
        >
          Sign in
        </button>
      }
      @case ('failed') {
        <p role="alert">
          This confirmation link didn’t work. It may have expired. Create your account again to get a new link.
        </p>
      }
    }
  `,
})
export class ConfirmPage {
  protected readonly auth = inject(AuthService);
  private readonly query = inject(ActivatedRoute).snapshot.queryParamMap;
  protected readonly state = signal<'working' | 'done' | 'failed'>('working');

  constructor() {
    const userId = this.query.get('userId');
    const code = this.query.get('code');
    if (!userId || !code) {
      this.state.set('failed');
      return;
    }
    firstValueFrom(inject(AuthApi).confirmEmail(userId, code)).then(
      () => this.state.set('done'),
      () => this.state.set('failed'),
    );
  }
}
