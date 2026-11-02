import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { BursaryStatus, CoverageItem, PagedResponse, StudyLevel } from './models';
import { toHttpParams } from './query-params';

export const REVIEW_TYPES = [
  { value: 'NewListing', label: 'New listing' },
  { value: 'LowConfidence', label: 'Low confidence' },
  { value: 'PossibleDuplicate', label: 'Possible duplicate' },
  { value: 'Vanished', label: 'Vanished' },
  { value: 'UserReport', label: 'User report' },
] as const;

export type ReviewType = (typeof REVIEW_TYPES)[number]['value'];
export type ReviewAction = 'Publish' | 'Dismiss' | 'MarkClosed';

/** `GET /api/v1/admin/review-items` (T-043). */
export interface ReviewItem {
  id: string;
  type: ReviewType;
  status: string;
  createdAt: string;
  bursaryId: string | null;
  bursaryName: string | null;
  funderName: string | null;
  isPublished: boolean | null;
  bursaryStatus: BursaryStatus | null;
  closingDate: string | null;
  reason: Record<string, unknown> | null;
  reportReason: string | null;
  reportComment: string | null;
}

/** `GET /api/v1/admin/bursaries` (T-044). */
export interface AdminBursarySummary {
  id: string;
  slug: string;
  name: string;
  funderName: string | null;
  isPublished: boolean;
  status: BursaryStatus;
  closingDate: string | null;
}

/** `GET /api/v1/admin/bursaries/{id}` (T-044). `lockedFields` holds lock names such as `FunderId` and `Eligibility`. */
export interface AdminBursary extends AdminBursarySummary {
  summary: string | null;
  openDate: string | null;
  studyLevels: StudyLevel[];
  minAverage: number | null;
  coverage: CoverageItem[];
  fieldCodes: string[];
  applyUrl: string | null;
  lockedFields: string[];
}

/** Body of `PATCH /api/v1/admin/bursaries/{id}`: any subset of these. Changed fields get locked by the API (not `isPublished`). */
export type AdminBursaryPatch = Partial<
  Pick<
    AdminBursary,
    | 'name'
    | 'funderName'
    | 'summary'
    | 'applyUrl'
    | 'openDate'
    | 'closingDate'
    | 'minAverage'
    | 'studyLevels'
    | 'coverage'
    | 'fieldCodes'
    | 'isPublished'
  >
>;

export type RunStatus = 'Running' | 'Succeeded' | 'PartiallyFailed' | 'Failed';

/** The latest run of one source (`lastRun` on `AdminSource`). */
export interface SourceRun {
  startedAt: string;
  finishedAt: string | null;
  status: RunStatus;
  discovered: number;
  created: number;
  updated: number;
  failed: number;
  errorSummary: string | null;
}

/** `GET /api/v1/admin/sources` (T-044). `lastRun` is null until the source has run. */
export interface AdminSource {
  id: string;
  key: string;
  name: string;
  enabled: boolean;
  lastRun: SourceRun | null;
}

/** `POST /api/v1/admin/ingestion-runs` (T-044) answers 202 with the queued request. */
export interface IngestionRunRequest {
  id: string;
  sourceId: string | null;
  requestedAt: string;
  pickedUpAt: string | null;
}

/** One source's counts inside an `IngestionRun`. */
export interface RunSource {
  sourceId: string;
  sourceKey: string;
  sourceName: string;
  discovered: number;
  created: number;
  updated: number;
  failed: number;
  errorSummary: string | null;
}

/** `GET /api/v1/admin/ingestion-runs` (T-044): one row of run history. */
export interface IngestionRun {
  id: string;
  trigger: 'Scheduled' | 'Manual';
  status: RunStatus;
  startedAt: string;
  finishedAt: string | null;
  sources: RunSource[];
}

/** Admin endpoints (FR-41, FR-42). The API enforces the Admin role; the UI guard is convenience. */
@Injectable({ providedIn: 'root' })
export class AdminApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}/api/v1/admin`;

  reviewItems(type?: ReviewType): Observable<PagedResponse<ReviewItem>> {
    return this.http.get<PagedResponse<ReviewItem>>(`${this.url}/review-items`, {
      params: toHttpParams({ type }),
    });
  }

  resolve(id: string, action: ReviewAction, notes?: string): Observable<ReviewItem> {
    return this.http.post<ReviewItem>(
      `${this.url}/review-items/${encodeURIComponent(id)}/resolve`,
      { action, notes },
    );
  }

  bursaries(
    query: string,
    published: boolean | undefined,
    page: number,
  ): Observable<PagedResponse<AdminBursarySummary>> {
    return this.http.get<PagedResponse<AdminBursarySummary>>(`${this.url}/bursaries`, {
      params: toHttpParams({ query, published, page }),
    });
  }

  bursary(id: string): Observable<AdminBursary> {
    return this.http.get<AdminBursary>(`${this.url}/bursaries/${encodeURIComponent(id)}`);
  }

  update(id: string, changes: AdminBursaryPatch): Observable<AdminBursary> {
    return this.http.patch<AdminBursary>(
      `${this.url}/bursaries/${encodeURIComponent(id)}`,
      changes,
    );
  }

  unlock(id: string, field: string): Observable<AdminBursary> {
    return this.http.delete<AdminBursary>(
      `${this.url}/bursaries/${encodeURIComponent(id)}/locks/${encodeURIComponent(field)}`,
    );
  }

  sources(): Observable<AdminSource[]> {
    return this.http.get<AdminSource[]>(`${this.url}/sources`);
  }

  setSourceEnabled(id: string, enabled: boolean): Observable<AdminSource> {
    return this.http.patch<AdminSource>(`${this.url}/sources/${encodeURIComponent(id)}`, {
      enabled,
    });
  }

  /** No `sourceId` runs every enabled source. */
  requestRun(sourceId?: string): Observable<IngestionRunRequest> {
    return this.http.post<IngestionRunRequest>(`${this.url}/ingestion-runs`, { sourceId });
  }

  ingestionRuns(page: number): Observable<PagedResponse<IngestionRun>> {
    return this.http.get<PagedResponse<IngestionRun>>(`${this.url}/ingestion-runs`, {
      params: toHttpParams({ page }),
    });
  }
}
