import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { FacultyResponse, FieldOfStudyResponse, InstitutionResponse, QualificationResponse } from './models';
import { toHttpParams } from './query-params';

/** Read-only taxonomy endpoints that fill the degree picker and the field filter (FR-01). */
@Injectable({ providedIn: 'root' })
export class TaxonomyApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** Institutions with at least one qualification, or every institution when `includeAll` is true. */
  institutions(includeAll = false): Observable<InstitutionResponse[]> {
    const params = toHttpParams({ includeAll: includeAll ? 'true' : undefined });
    return this.http.get<InstitutionResponse[]>(`${this.baseUrl}/api/v1/taxonomy/institutions`, { params });
  }

  faculties(institutionId: string): Observable<FacultyResponse[]> {
    return this.http.get<FacultyResponse[]>(
      `${this.baseUrl}/api/v1/taxonomy/institutions/${encodeURIComponent(institutionId)}/faculties`,
    );
  }

  qualifications(facultyId: string): Observable<QualificationResponse[]> {
    return this.http.get<QualificationResponse[]>(
      `${this.baseUrl}/api/v1/taxonomy/faculties/${encodeURIComponent(facultyId)}/qualifications`,
    );
  }

  fields(): Observable<FieldOfStudyResponse[]> {
    return this.http.get<FieldOfStudyResponse[]>(`${this.baseUrl}/api/v1/taxonomy/fields`);
  }
}
