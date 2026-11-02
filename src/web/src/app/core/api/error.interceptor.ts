import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiError, ApiProblemError } from './api-error';

/** ProblemDetails members defined by RFC 7807. Every other member is an extension. */
const STANDARD_PROBLEM_KEYS = new Set(['type', 'title', 'status', 'detail', 'instance']);

/**
 * Converts every failed HTTP response into an {@link ApiError}. ASP.NET Core flattens ProblemDetails
 * extensions into the top-level JSON object, so `errors`, `bursary` and friends are read from there.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: unknown) =>
      throwError(() => (error instanceof HttpErrorResponse ? toApiError(error) : error)),
    ),
  );

export function toApiError(error: HttpErrorResponse): ApiError {
  if (error.status === 0) {
    return new ApiError(0, 'Network', 'Could not reach the server. Check your connection and try again.');
  }

  const body = isRecord(error.error) ? error.error : {};
  const errors = parseProblemErrors(body['errors']);
  const extensions: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(body)) {
    // `detail` stays readable here: Identity's login puts "NotAllowed" / "LockedOut" in it.
    if (!STANDARD_PROBLEM_KEYS.has(key) || key === 'detail') {
      extensions[key] = value;
    }
  }

  const title = typeof body['title'] === 'string' ? body['title'] : undefined;
  const detail = typeof body['detail'] === 'string' ? body['detail'] : undefined;
  const message = title ?? detail ?? `Request failed with status ${error.status}.`;

  return new ApiError(error.status, errors[0]?.code ?? 'Unknown', message, errors, extensions);
}

function parseProblemErrors(value: unknown): ApiProblemError[] {
  if (!Array.isArray(value)) {
    return [];
  }
  return value
    .filter(isRecord)
    .filter((item): item is Record<string, unknown> & { code: string } => typeof item['code'] === 'string')
    .map((item) => ({
      code: item.code,
      message: typeof item['message'] === 'string' ? item['message'] : '',
    }));
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
