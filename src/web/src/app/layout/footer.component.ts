import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  template: `
    <footer class="mt-8 border-t border-line">
      <div class="mx-auto max-w-5xl px-4 py-6 text-sm text-muted md:px-6">
        <nav class="mb-2 flex gap-4">
          <a routerLink="/about" class="text-jacaranda underline hover:text-ink">About</a>
          <a routerLink="/privacy" class="text-jacaranda underline hover:text-ink">Privacy</a>
        </nav>
        <p>Bursary info is gathered from public sources. Always confirm details on the funder's site.</p>
      </div>
    </footer>
  `,
})
export class FooterComponent {}
