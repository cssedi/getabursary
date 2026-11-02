import { InjectionToken } from '@angular/core';

/** Base URL for API calls: '' in the browser (same origin / dev proxy), API_INTERNAL_URL on the server. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '',
});
