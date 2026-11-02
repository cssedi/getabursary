import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  template: `
    <footer class="bg-surface border-t border-surface-alt mt-8">
      <div class="mx-auto max-w-5xl px-4 py-6 text-sm text-muted">
        <nav class="flex gap-4 mb-2">
          <a routerLink="/about" class="text-primary underline">About</a>
          <a routerLink="/privacy" class="text-primary underline">Privacy</a>
        </nav>
        <p>Bursary info is gathered from public sources. Always confirm details on the funder's site.</p>
      </div>
    </footer>
  `,
})
export class FooterComponent {}
