import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-header',
  imports: [RouterLink],
  template: `
    <header class="bg-surface border-b border-surface-alt">
      <div class="mx-auto max-w-5xl px-4 py-3">
        <a routerLink="/" class="text-xl font-bold text-primary">GetABursary</a>
      </div>
    </header>
  `,
})
export class HeaderComponent {}
