import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  template: `
    <footer class="mt-16 border-t border-line">
      <div class="mx-auto max-w-300 px-4 py-8 text-sm text-muted md:px-6 xl:px-8">
        <nav aria-label="Footer" class="mb-2 flex gap-4">
          <a routerLink="/about" class="text-jacaranda underline hover:text-ink active:text-ink">About</a>
          <a routerLink="/privacy" class="text-jacaranda underline hover:text-ink active:text-ink">Privacy</a>
        </nav>
        <p>Listings come from public bursary sites. Always confirm on the funder’s site.</p>
      </div>
    </footer>
  `,
})
export class FooterComponent {}
