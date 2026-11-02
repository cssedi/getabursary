import { Injectable, Injector, REQUEST, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthApi } from '../api/auth-api';
import { MeResponse } from '../api/models';

/** A "save"-style action that needs an account, e.g. `{ type: 'save', bursaryId }`. Must be JSON-safe. */
export interface PendingAction {
  type: string;
  [key: string]: unknown;
}

const STORAGE_KEY = 'pendingAuthAction';

/** Signed-in state (loaded from `GET /me`) and the FR-09 "sign in, then finish what you were doing" flow. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(AuthApi);
  private readonly injector = inject(Injector);
  private readonly request = inject(REQUEST, { optional: true });
  private readonly handlers = new Map<string, (action: PendingAction) => void>();
  private loading: Promise<void> | undefined;

  private readonly _user = signal<MeResponse | null>(null);
  readonly user = this._user.asReadonly();
  readonly isAdmin = computed(() => this._user()?.roles.includes('Admin') ?? false);

  /** Loads `/me` once. Any failure (401 included) means signed out. */
  load(): Promise<void> {
    this.loading ??= firstValueFrom(this.api.me(this.request?.headers.get('cookie')))
      .then((me) => this._user.set(me))
      .catch(() => this._user.set(null))
      .then(() => this.replay());
    return this.loading;
  }

  async signIn(email: string, password: string): Promise<void> {
    await firstValueFrom(this.api.login(email, password));
    this._user.set(await firstValueFrom(this.api.me()));
    this.replay();
  }

  async signUp(email: string, password: string): Promise<void> {
    await firstValueFrom(this.api.register(email, password));
  }

  async signOut(): Promise<void> {
    await firstValueFrom(this.api.logout());
    this._user.set(null);
  }

  /** Registers what to do for an action type. Replays at once if that action was waiting from before a redirect. */
  handle(type: string, run: (action: PendingAction) => void): void {
    this.handlers.set(type, run);
    if (this._user() && this.readPending()?.type === type) this.replay();
  }

  /** Runs the action now if signed in; otherwise parks it, opens the dialog and replays after sign-in. */
  requireAuth(action: PendingAction): void {
    if (this._user()) {
      this.handlers.get(action.type)?.(action);
      return;
    }
    // shortcut: sessionStorage is per tab, so confirming the email in another tab won't replay the action.
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(action));
    } catch {
      // Storage blocked: the dialog still works, the action just won't survive a redirect.
    }
    void this.openDialog();
  }

  /** The header's "Sign in": nothing is waiting to be replayed. */
  openSignIn(): void {
    this.clearPending();
    void this.openDialog();
  }

  clearPending(): void {
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // Nothing stored.
    }
  }

  private async openDialog(): Promise<void> {
    const { SignInDialogComponent } = await import('../../features/account/sign-in-dialog.component');
    // Lazy: the CDK dialog stack is ~80 kB and most visits never sign in.
    const { Dialog } = await import('@angular/cdk/dialog');
    this.injector.get(Dialog).open(SignInDialogComponent, {
      ariaLabel: 'Sign in',
      width: '420px',
      maxWidth: 'calc(100vw - 32px)',
      panelClass: 'auth-dialog-panel',
      backdropClass: 'auth-dialog-backdrop',
    });
  }

  private readPending(): PendingAction | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as PendingAction) : null;
    } catch {
      return null;
    }
  }

  private replay(): void {
    const action = this.readPending();
    const run = action && this._user() ? this.handlers.get(action.type) : undefined;
    if (!action || !run) return;
    this.clearPending();
    run(action);
  }
}
