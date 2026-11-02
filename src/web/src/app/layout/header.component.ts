import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

const MENU_ITEM = 'flex min-h-11 w-full items-center px-3 text-left hover:bg-bloom active:bg-bloom';

@Component({
  selector: 'app-header',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="border-b border-line">
      <div class="mx-auto flex max-w-5xl items-center justify-between gap-4 px-4 py-3 md:px-6">
        <a routerLink="/" class="text-xl font-extrabold text-ink hover:text-jacaranda">GetABursary</a>
        @if (auth.user(); as user) {
          <details #menu class="relative">
            <summary
              class="flex min-h-11 max-w-56 cursor-pointer items-center rounded-control px-3 font-medium hover:bg-bloom active:bg-bloom"
            >
              <span class="truncate">{{ user.email }}</span>
            </summary>
            <div class="absolute right-0 z-20 mt-1 w-56 rounded-control border border-line bg-paper py-1">
              <a routerLink="/my-bursaries" [class]="item" (click)="menu.open = false">My bursaries</a>
              @if (auth.isAdmin()) {
                <a routerLink="/admin" [class]="item" (click)="menu.open = false">Admin</a>
              }
              <button type="button" [class]="item" (click)="menu.open = false; auth.signOut()">Sign out</button>
            </div>
          </details>
        } @else {
          <button
            type="button"
            class="min-h-11 rounded-control border border-line px-4 font-medium hover:bg-bloom active:bg-bloom"
            (click)="auth.openSignIn()"
          >
            Sign in
          </button>
        }
      </div>
    </header>
  `,
})
export class HeaderComponent {
  protected readonly auth = inject(AuthService);
  protected readonly item = MENU_ITEM;
}
