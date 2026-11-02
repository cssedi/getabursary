import { HttpParams } from '@angular/common/http';

/**
 * Builds query parameters from an object. `undefined`, `null` and `''` are omitted, and an array
 * repeats its key once per item (`fieldIds=1&fieldIds=2`). An empty array adds nothing.
 */
export function toHttpParams(values: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(values)) {
    const items: unknown[] = Array.isArray(value) ? value : [value];
    for (const item of items) {
      if (item === undefined || item === null || item === '') {
        continue;
      }
      params = params.append(key, String(item));
    }
  }
  return params;
}
