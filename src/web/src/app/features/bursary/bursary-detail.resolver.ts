import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { catchError, of } from 'rxjs';
import { ApiError } from '../../core/api/api-error';
import { BursaryApi, BursaryDetailResult } from '../../core/api/bursary-api';

/** What the detail page renders: an open or closed bursary, an unknown slug, or a failed load. */
export type BursaryPageData = BursaryDetailResult | { kind: 'notFound' } | { kind: 'error' };

/** Loads `GET /api/v1/bursaries/:slug` before the page renders, so SSR has the data in the HTML. */
export const bursaryDetailResolver: ResolveFn<BursaryPageData> = (route) => {
  const api = inject(BursaryApi);
  return api
    .bySlug(route.paramMap.get('slug') ?? '')
    .pipe(
      catchError((error: unknown) =>
        of<BursaryPageData>(
          error instanceof ApiError && error.status === 404
            ? { kind: 'notFound' }
            : { kind: 'error' },
        ),
      ),
    );
};
