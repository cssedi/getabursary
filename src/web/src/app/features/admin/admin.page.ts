import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SeoService } from '../seo/seo.service';

const LINK =
  'flex min-h-11 items-center border-b-2 border-transparent px-3 hover:bg-bloom active:bg-bloom';

/** `/admin` layout: sub-nav and outlet. Never indexed. */
@Component({
  selector: 'app-admin-page',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-2xl font-bold">Admin</h1>
    <nav aria-label="Admin" class="mt-2 flex flex-wrap border-b border-line">
      <a
        routerLink="/admin"
        [routerLinkActiveOptions]="{ exact: true }"
        routerLinkActive="border-jacaranda font-semibold"
        [class]="link"
        >Review queue</a
      >
      <a
        routerLink="/admin/bursaries"
        routerLinkActive="border-jacaranda font-semibold"
        [class]="link"
        >Bursaries</a
      >
      <a
        routerLink="/admin/sources"
        routerLinkActive="border-jacaranda font-semibold"
        [class]="link"
        >Sources</a
      >
    </nav>
    <div class="mt-4"><router-outlet /></div>
  `,
})
export class AdminPage {
  protected readonly link = LINK;

  constructor() {
    inject(SeoService).set({
      title: 'Admin',
      description: 'Admin area',
      canonicalPath: '/admin',
      robots: 'noindex',
    });
  }
}
