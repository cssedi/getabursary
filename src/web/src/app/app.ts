import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';
import { FooterComponent } from './layout/footer.component';
import { HeaderComponent } from './layout/header.component';

@Component({
  imports: [RouterOutlet, HeaderComponent, FooterComponent],
  selector: 'app-root',
  template: `
    <a class="skip-link" href="#content" (click)="skipToContent($event)">Skip to content</a>
    <app-header />
    <main id="content" tabindex="-1" class="mx-auto max-w-5xl px-4 py-6 md:px-6"><router-outlet /></main>
    <app-footer />
  `,
})
export class App {
  constructor() {
    void inject(AuthService).load();
  }

  /** `<base href="/">` would turn a bare `#content` link into a navigation, so move focus directly. */
  protected skipToContent(event: Event): void {
    event.preventDefault();
    document.getElementById('content')?.focus();
  }
}
