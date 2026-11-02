import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { reportRequest } from '../../../testing/fixtures';
import { ApiError } from './api-error';
import { errorInterceptor } from './error.interceptor';
import { ReportApi } from './report-api';

describe('ReportApi', () => {
  let api: ReportApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withFetch(), withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    api = TestBed.inject(ReportApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('create_validRequest_postsBodyToBursaryReportsRoute', () => {
    const bursaryId = '0196a5f0-0000-7000-8000-000000000101';
    let result: unknown;
    api.create(bursaryId, reportRequest).subscribe((value) => (result = value));

    const req = http.expectOne(`/api/v1/bursaries/${bursaryId}/reports`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(reportRequest);
    req.flush({ id: 'report-1' }, { status: 201, statusText: 'Created' });
    expect(result).toEqual({ id: 'report-1' });
  });

  it('create_429_throwsApiErrorWithStatus', () => {
    let error: unknown;
    api.create('b-1', reportRequest).subscribe({ error: (e: unknown) => (error = e) });

    http.expectOne('/api/v1/bursaries/b-1/reports').flush(
      { title: 'Too many reports', errors: [{ code: 'RateLimit.Exceeded', message: 'Slow down' }] },
      { status: 429, statusText: 'Too Many Requests' },
    );
    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 429, code: 'RateLimit.Exceeded' });
  });

  it('create_400_keepsEveryFieldErrorInErrors', () => {
    let error: ApiError | undefined;
    api.create('b-1', { reason: 'Other', contactEmail: 'bad' }).subscribe({
      error: (e: unknown) => (error = e as ApiError),
    });

    http.expectOne('/api/v1/bursaries/b-1/reports').flush(
      {
        title: 'Invalid email',
        errors: [
          { code: 'Report.InvalidEmail', message: 'Email is not valid.' },
          { code: 'Report.CommentTooLong', message: 'Comment is too long.' },
        ],
      },
      { status: 400, statusText: 'Bad Request' },
    );
    expect(error?.code).toBe('Report.InvalidEmail');
    expect(error?.errors.map((e) => e.code)).toEqual(['Report.InvalidEmail', 'Report.CommentTooLong']);
  });
});
