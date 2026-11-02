/** One entry of the `errors` extension on a ProblemDetails response (`ProblemError` in the API). */
export interface ApiProblemError {
  code: string;
  message: string;
}

/**
 * The single error type the web app sees for failed API calls.
 *
 * - `status`: HTTP status, or `0` for a network failure.
 * - `code`: first error code from the ProblemDetails `errors`, `'Network'` for a network failure,
 *   or `'Unknown'` when the response carried no error code.
 * - `errors`: every error from the ProblemDetails `errors` extension.
 * - `extensions`: the raw ProblemDetails extension members (e.g. `bursary` on a `410`).
 */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string,
    message: string,
    readonly errors: readonly ApiProblemError[] = [],
    readonly extensions: Readonly<Record<string, unknown>> = {},
  ) {
    super(message);
    this.name = 'ApiError';
  }
}
