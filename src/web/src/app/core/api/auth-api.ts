import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { MeResponse } from './models';

/** Identity's cookie endpoints under `/api/v1/auth` (ADR-003) and `GET /me`. */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** On the server `cookie` is the visitor's cookie header, so the render sees the visitor's session. */
  me(cookie?: string | null): Observable<MeResponse> {
    const headers = cookie ? new HttpHeaders({ Cookie: cookie }) : undefined;
    return this.http.get<MeResponse>(`${this.baseUrl}/api/v1/me`, { headers });
  }

  login(email: string, password: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/api/v1/auth/login?useCookies=true`, { email, password });
  }

  register(email: string, password: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/api/v1/auth/register`, { email, password });
  }

  logout(): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/api/v1/auth/logout`, {});
  }

  confirmEmail(userId: string, code: string): Observable<unknown> {
    return this.http.get(`${this.baseUrl}/api/v1/auth/confirmEmail`, { params: { userId, code } });
  }

  forgotPassword(email: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/api/v1/auth/forgotPassword`, { email });
  }

  resetPassword(email: string, resetCode: string, newPassword: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/api/v1/auth/resetPassword`, { email, resetCode, newPassword });
  }
}
