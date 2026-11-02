import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FooterComponent } from './layout/footer.component';
import { HeaderComponent } from './layout/header.component';

@Component({
  imports: [RouterOutlet, HeaderComponent, FooterComponent],
  selector: 'app-root',
  template: `
    <app-header />
    <main class="mx-auto max-w-5xl px-4 py-6"><router-outlet /></main>
    <app-footer />
  `,
})
export class App {}
