import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { faculties, fields, institutions, qualifications } from '../../../testing/fixtures';
import { errorInterceptor } from './error.interceptor';
import { TaxonomyApi } from './taxonomy-api';

describe('TaxonomyApi', () => {
  let api: TaxonomyApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withFetch(), withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    api = TestBed.inject(TaxonomyApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('institutions_default_requestsWithoutIncludeAll', () => {
    let result: unknown;
    api.institutions().subscribe((value) => (result = value));

    const req = http.expectOne((r) => r.url === '/api/v1/taxonomy/institutions');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    req.flush(institutions);
    expect(result).toEqual(institutions);
  });

  it('institutions_includeAllTrue_sendsIncludeAllQuery', () => {
    api.institutions(true).subscribe();

    const req = http.expectOne((r) => r.url === '/api/v1/taxonomy/institutions');
    expect(req.request.params.get('includeAll')).toBe('true');
    req.flush([]);
  });

  it('institutions_includeAllFalse_omitsQuery', () => {
    api.institutions(false).subscribe();

    const req = http.expectOne((r) => r.url === '/api/v1/taxonomy/institutions');
    expect(req.request.params.has('includeAll')).toBe(false);
    req.flush([]);
  });

  it('faculties_institutionId_requestsNestedRoute', () => {
    const id = institutions[0].id;
    api.faculties(id).subscribe();

    const req = http.expectOne(`/api/v1/taxonomy/institutions/${id}/faculties`);
    expect(req.request.method).toBe('GET');
    req.flush(faculties);
  });

  it('qualifications_facultyId_requestsNestedRoute', () => {
    const id = faculties[0].id;
    api.qualifications(id).subscribe();

    const req = http.expectOne(`/api/v1/taxonomy/faculties/${id}/qualifications`);
    expect(req.request.method).toBe('GET');
    req.flush(qualifications);
  });

  it('qualificationsByInstitution_institutionId_requestsOneGroupedRoute', () => {
    const id = institutions[0].id;
    api.qualificationsByInstitution(id).subscribe();

    const req = http.expectOne(`/api/v1/taxonomy/institutions/${id}/qualifications`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('fields_always_requestsFieldTree', () => {
    let result: unknown;
    api.fields().subscribe((value) => (result = value));

    const req = http.expectOne('/api/v1/taxonomy/fields');
    expect(req.request.method).toBe('GET');
    req.flush(fields);
    expect(result).toEqual(fields);
  });

  it('institutions_serverError_throwsApiError', () => {
    let error: unknown;
    api.institutions().subscribe({ error: (e: unknown) => (error = e) });

    http
      .expectOne((r) => r.url === '/api/v1/taxonomy/institutions')
      .flush({ title: 'Boom', errors: [{ code: 'Unexpected.Failure', message: 'Boom' }] }, {
        status: 500,
        statusText: 'Server Error',
      });
    expect(error).toMatchObject({ name: 'ApiError', status: 500, code: 'Unexpected.Failure' });
  });
});
