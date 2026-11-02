import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { TrackerEntryResponse, UpsertTrackerRequest } from './models';

/** Saved bursaries and the application tracker (FR-12 to FR-14). Signed in only. */
@Injectable({ providedIn: 'root' })
export class TrackerApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  list(): Observable<TrackerEntryResponse[]> {
    return this.http.get<TrackerEntryResponse[]>(`${this.baseUrl}/api/v1/me/tracker`);
  }

  upsert(bursaryId: string, request: UpsertTrackerRequest): Observable<TrackerEntryResponse> {
    return this.http.put<TrackerEntryResponse>(`${this.baseUrl}/api/v1/me/tracker/${encodeURIComponent(bursaryId)}`, request);
  }

  remove(bursaryId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/v1/me/tracker/${encodeURIComponent(bursaryId)}`);
  }
}
