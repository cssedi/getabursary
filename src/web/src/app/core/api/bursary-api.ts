import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { ApiError } from './api-error';
import { BursaryDetailResponse, BursarySearchParams, BursarySummaryResponse, PagedResponse } from './models';
import { toHttpParams } from './query-params';

/** Result of loading a bursary by slug. A closed bursary (`410`) carries only its summary. */
export type BursaryDetailResult =
  | { kind: 'open'; bursary: BursaryDetailResponse }
  | { kind: 'closed'; summary: BursarySummaryResponse };

/** Bursary search and detail endpoints (FR-01 to FR-07). */
@Injectable({ providedIn: 'root' })
export class BursaryApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  search(params: BursarySearchParams = {}): Observable<PagedResponse<BursarySummaryResponse>> {
    return this.http.get<PagedResponse<BursarySummaryResponse>>(`${this.baseUrl}/api/v1/bursaries/search`, {
      params: toHttpParams(params),
    });
  }

  /**
   * Open bursaries resolve to `{ kind: 'open' }`. A `410 Gone` with the `bursary` summary resolves to
   * `{ kind: 'closed' }`. Any other failure is re-thrown as an {@link ApiError}.
   */
  bySlug(slug: string): Observable<BursaryDetailResult> {
    const url = `${this.baseUrl}/api/v1/bursaries/${encodeURIComponent(slug)}`;
    return this.http.get<BursaryDetailResponse>(url).pipe(
      map((bursary): BursaryDetailResult => ({ kind: 'open', bursary })),
      catchError((error: unknown) => {
        const summary = error instanceof ApiError && error.status === 410 ? error.extensions['bursary'] : undefined;
        if (summary !== undefined && summary !== null && typeof summary === 'object') {
          return of<BursaryDetailResult>({ kind: 'closed', summary: summary as BursarySummaryResponse });
        }
        return throwError(() => error);
      }),
    );
  }
}
