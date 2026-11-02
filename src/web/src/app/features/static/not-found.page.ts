import { Component, RESPONSE_INIT, inject } from '@angular/core';

@Component({
  selector: 'app-not-found-page',
  template: `
    <h1 class="text-2xl font-bold">Page not found</h1>
    <p class="mt-2">Check the address, or go back to the home page.</p>
  `,
})
export class NotFoundPage {
  constructor() {
    // Only provided during SSR; sets the HTTP status of the rendered response.
    const init = inject(RESPONSE_INIT, { optional: true });
    if (init) {
      init.status = 404;
    }
  }
}
