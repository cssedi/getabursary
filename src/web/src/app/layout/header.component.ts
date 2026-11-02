import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-header',
  imports: [RouterLink],
  template: `
    <header class="border-b border-line">
      <div class="mx-auto max-w-5xl px-4 py-3 md:px-6">
        <a routerLink="/" class="text-xl font-extrabold text-ink hover:text-jacaranda">GetABursary</a>
      </div>
    </header>
  `,
})
export class HeaderComponent {}
