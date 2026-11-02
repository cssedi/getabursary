import { HttpClient, provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiError } from './api-error';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withFetch(), withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('problemDetails400_convertsToApiErrorWithFirstCode', () => {
    let error: unknown;
    client.get('/api/v1/bursaries/search').subscribe({ error: (e: unknown) => (error = e) });

    http.expectOne('/api/v1/bursaries/search').flush(
      {
        type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
        title: 'Unknown qualification.',
        status: 400,
        errors: [{ code: 'Search.UnknownQualification', message: 'Unknown qualification.' }],
      },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(error).toBeInstanceOf(ApiError);
    const apiError = error as ApiError;
    expect(apiError.status).toBe(400);
    expect(apiError.code).toBe('Search.UnknownQualification');
    expect(apiError.message).toBe('Unknown qualification.');
    expect(apiError.errors).toEqual([{ code: 'Search.UnknownQualification', message: 'Unknown qualification.' }]);
  });

  it('problemDetails_extensionMembers_areExposedRaw', () => {
    let error: ApiError | undefined;
    client.get('/x').subscribe({ error: (e: unknown) => (error = e as ApiError) });

    http.expectOne('/x').flush(
      { title: 'Gone', errors: [{ code: 'Bursary.Closed', message: 'Closed' }], bursary: { id: 'b-1' } },
      { status: 410, statusText: 'Gone' },
    );

    expect(error?.extensions['bursary']).toEqual({ id: 'b-1' });
    expect(error?.extensions['errors']).toEqual([{ code: 'Bursary.Closed', message: 'Closed' }]);
    expect(error?.extensions['title']).toBeUndefined();
  });

  it('networkError_convertsToCodeNetworkWithStatusZero', () => {
    let error: ApiError | undefined;
    client.get('/api/v1/taxonomy/fields').subscribe({ error: (e: unknown) => (error = e as ApiError) });

    http.expectOne('/api/v1/taxonomy/fields').error(new ProgressEvent('error'));

    expect(error).toBeInstanceOf(ApiError);
    expect(error?.code).toBe('Network');
    expect(error?.status).toBe(0);
    expect(error?.errors).toEqual([]);
  });

  it('nonProblemBody_usesStatusMessageAndUnknownCode', () => {
    let error: ApiError | undefined;
    client.get('/x').subscribe({ error: (e: unknown) => (error = e as ApiError) });

    http.expectOne('/x').flush('<html>Bad gateway</html>', { status: 502, statusText: 'Bad Gateway' });

    expect(error?.status).toBe(502);
    expect(error?.code).toBe('Unknown');
    expect(error?.message).toBe('Request failed with status 502.');
  });

  it('success_passesResponseThroughUnchanged', () => {
    let body: unknown;
    client.get<{ ok: boolean }>('/x').subscribe((value) => (body = value));

    http.expectOne('/x').flush({ ok: true });

    expect(body).toEqual({ ok: true });
  });
});
