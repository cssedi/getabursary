import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

const LINK = 'flex min-h-11 items-center rounded-control px-3 font-medium hover:bg-bloom active:bg-bloom';
const MENU_ITEM = 'flex min-h-11 w-full items-center px-3 text-left hover:bg-bloom active:bg-bloom';

@Component({
  selector: 'app-header',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div #sentinel aria-hidden="true" class="h-px"></div>
    <header
      class="sticky top-0 z-30 border-b bg-paper"
      [class.border-line]="scrolled()"
      [class.border-transparent]="!scrolled()"
    >
      <div class="mx-auto flex max-w-300 items-center justify-between gap-4 px-4 py-2 md:px-6 xl:px-8">
        <a routerLink="/" class="flex min-h-11 items-center text-xl font-bold lowercase text-ink hover:text-jacaranda"
          >getabursary</a
        >
        <nav aria-label="Main" class="hidden items-center gap-1 sm:flex">
          <a routerLink="/search" [class]="link">Search</a>
          @if (auth.user(); as user) {
            <a routerLink="/my-bursaries" [class]="link">My bursaries</a>
            <details #menu class="relative">
              <summary [class]="link + ' max-w-56 cursor-pointer'">
                <span class="truncate">{{ user.email }}</span>
              </summary>
              <div class="absolute right-0 z-40 mt-1 w-56 rounded-control border border-line bg-paper-raised py-1">
                @if (auth.isAdmin()) {
                  <a routerLink="/admin" [class]="item" (click)="menu.open = false">Admin</a>
                }
                <button type="button" [class]="item" (click)="menu.open = false; auth.signOut()">Sign out</button>
              </div>
            </details>
          } @else {
            <button type="button" class="btn-secondary" (click)="auth.openSignIn()">Sign in</button>
          }
        </nav>
        <button
          type="button"
          class="btn-secondary sm:hidden"
          aria-haspopup="dialog"
          (click)="openMenu()"
        >
          Menu
        </button>
      </div>
    </header>
  `,
})
export class HeaderComponent {
  protected readonly auth = inject(AuthService);
  private readonly injector = inject(Injector);
  protected readonly link = LINK;
  protected readonly item = MENU_ITEM;
  /** True once the page has scrolled past the sentinel: the header then gets its hairline. */
  protected readonly scrolled = signal(false);
  private menuOpen = false;
  private readonly sentinel = viewChild.required<ElementRef<HTMLElement>>('sentinel');

  constructor() {
    afterNextRender(() => {
      if (typeof IntersectionObserver === 'undefined') return;
      new IntersectionObserver(([entry]) => this.scrolled.set(!entry.isIntersecting)).observe(
        this.sentinel().nativeElement,
      );
    });
  }

  protected async openMenu(): Promise<void> {
    if (this.menuOpen) return;
    this.menuOpen = true;
    // Lazy: the CDK dialog stack is ~80 kB and most desktop visits never open the sheet.
    let modules;
    try {
      modules = await Promise.all([
        import('./mobile-nav.component'),
        import('@angular/cdk/dialog'),
        import('@angular/cdk/overlay'),
      ]);
    } catch (error) {
      this.menuOpen = false;
      throw error;
    }
    const [{ MobileNavComponent }, { Dialog }, { Overlay }] = modules;
    const ref = this.injector.get(Dialog).open(MobileNavComponent, {
      ariaLabel: 'Menu',
      autoFocus: 'dialog', // no autofocus into a control on mobile
      width: 'calc(100% - 16px)',
      maxWidth: '24rem',
      panelClass: 'nav-sheet-panel',
      backdropClass: 'nav-sheet-backdrop',
      positionStrategy: this.injector.get(Overlay).position().global().top('8px').centerHorizontally(),
    });
    ref.closed.subscribe(() => (this.menuOpen = false));
  }
}
