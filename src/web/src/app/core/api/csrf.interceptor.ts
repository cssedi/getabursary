import { HttpInterceptorFn } from '@angular/common/http';

/** ADR-003: the API rejects POST/PUT/DELETE without `X-Requested-With: getabursary`. */
export const csrfInterceptor: HttpInterceptorFn = (req, next) =>
  next(
    req.method === 'GET' || req.method === 'HEAD'
      ? req
      : req.clone({ setHeaders: { 'X-Requested-With': 'getabursary' } }),
  );
