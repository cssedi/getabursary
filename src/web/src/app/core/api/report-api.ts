import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { CreateReportRequest, CreateReportResponse } from './models';

/** Report-an-inaccuracy endpoint (FR-08). Anonymous, rate limited by the API (429). */
@Injectable({ providedIn: 'root' })
export class ReportApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  create(bursaryId: string, request: CreateReportRequest): Observable<CreateReportResponse> {
    return this.http.post<CreateReportResponse>(
      `${this.baseUrl}/api/v1/bursaries/${encodeURIComponent(bursaryId)}/reports`,
      request,
    );
  }
}
