import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, ViewEncapsulation, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

const ITEM = 'flex min-h-12 w-full items-center px-2 text-left text-lg font-semibold hover:bg-bloom active:bg-bloom';

/** The header nav below 640 px: a sheet (CDK dialog: focus trapped, Esc closes). */
@Component({
  selector: 'app-mobile-nav',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  // None: the dialog panel lives outside this component's view.
  encapsulation: ViewEncapsulation.None,
  template: `
    <div class="mb-2 flex items-center justify-between">
      <h2 class="text-lg">Menu</h2>
      <button type="button" class="btn-secondary" (click)="ref.close()">Close</button>
    </div>
    <nav aria-label="Main" class="divide-y divide-line">
      <a routerLink="/search" [class]="item" (click)="ref.close()">Search</a>
      @if (auth.user(); as user) {
        <a routerLink="/my-bursaries" [class]="item" (click)="ref.close()">My bursaries</a>
        @if (auth.isAdmin()) {
          <a routerLink="/admin" [class]="item" (click)="ref.close()">Admin</a>
        }
        <p class="truncate px-2 py-3 text-muted">{{ user.email }}</p>
        <button type="button" [class]="item" (click)="signOut()">Sign out</button>
      } @else {
        <button type="button" [class]="item" (click)="signIn()">Sign in</button>
      }
    </nav>
  `,
  styles: `
    .nav-sheet-backdrop {
      background: rgb(27 26 23 / 0.5);
    }
    .nav-sheet-panel app-mobile-nav {
      display: block;
      box-sizing: border-box;
      max-height: 90dvh;
      overflow-y: auto;
      overscroll-behavior: contain;
      padding: 16px 16px calc(16px + env(safe-area-inset-bottom));
      border-radius: 16px;
      background: var(--paper-raised);
      box-shadow: 0 8px 32px rgb(27 26 23 / 0.18);
    }
  `,
})
export class MobileNavComponent {
  protected readonly ref = inject(DialogRef);
  protected readonly auth = inject(AuthService);
  protected readonly item = ITEM;

  protected signIn(): void {
    this.ref.close();
    this.auth.openSignIn();
  }

  protected signOut(): void {
    this.ref.close();
    void this.auth.signOut();
  }
}
