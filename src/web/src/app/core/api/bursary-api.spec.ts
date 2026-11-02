import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  bursaryDetail,
  closedBursary,
  searchPage,
} from '../../../testing/fixtures';
import { ApiError } from './api-error';
import { BursaryApi, BursaryDetailResult } from './bursary-api';
import { errorInterceptor } from './error.interceptor';

describe('BursaryApi', () => {
  let api: BursaryApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withFetch(), withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    api = TestBed.inject(BursaryApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  describe('search', () => {
    it('search_noParams_requestsBareSearchUrl', () => {
      let result: unknown;
      api.search().subscribe((value) => (result = value));

      const req = http.expectOne((r) => r.url === '/api/v1/bursaries/search');
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys()).toEqual([]);
      req.flush(searchPage);
      expect(result).toEqual(searchPage);
    });

    it('search_emptyAndUndefinedValues_omitsThem', () => {
      api
        .search({
          qualificationId: '',
          institutionId: undefined,
          fieldIds: [],
          average: undefined,
          studyLevel: undefined,
          page: 1,
        })
        .subscribe();

      const req = http.expectOne((r) => r.url === '/api/v1/bursaries/search');
      expect(req.request.params.keys()).toEqual(['page']);
      expect(req.request.params.get('page')).toBe('1');
      req.flush(searchPage);
    });

    it('search_fullParams_sendsEveryQueryKey', () => {
      api
        .search({
          qualificationId: 'q-1',
          fieldIds: ['08', '0801'],
          institutionId: 'i-1',
          studyLevel: 'Undergraduate',
          average: 72.5,
          closingFrom: '2026-11-01',
          closingTo: '2027-01-31',
          includeClosed: false,
          sort: 'ClosingSoonest',
          page: 2,
          pageSize: 10,
        })
        .subscribe();

      const req = http.expectOne((r) => r.url === '/api/v1/bursaries/search');
      const params = req.request.params;
      expect(params.get('qualificationId')).toBe('q-1');
      expect(params.get('institutionId')).toBe('i-1');
      expect(params.get('studyLevel')).toBe('Undergraduate');
      expect(params.get('average')).toBe('72.5');
      expect(params.get('closingFrom')).toBe('2026-11-01');
      expect(params.get('closingTo')).toBe('2027-01-31');
      expect(params.get('includeClosed')).toBe('false');
      expect(params.get('sort')).toBe('ClosingSoonest');
      expect(params.get('page')).toBe('2');
      expect(params.get('pageSize')).toBe('10');
      req.flush(searchPage);
    });

    it('search_fieldIds_repeatsKeyForEachId', () => {
      api.search({ fieldIds: ['08', '0801', '14'] }).subscribe();

      const req = http.expectOne((r) => r.url === '/api/v1/bursaries/search');
      expect(req.request.params.getAll('fieldIds')).toEqual(['08', '0801', '14']);
      expect(req.request.urlWithParams).toBe(
        '/api/v1/bursaries/search?fieldIds=08&fieldIds=0801&fieldIds=14',
      );
      req.flush(searchPage);
    });
  });

  describe('bySlug', () => {
    it('bySlug_200_mapsToOpenWithBursary', () => {
      let result: BursaryDetailResult | undefined;
      api.bySlug(bursaryDetail.slug).subscribe((value) => (result = value));

      http.expectOne(`/api/v1/bursaries/${bursaryDetail.slug}`).flush(bursaryDetail);
      expect(result).toEqual({ kind: 'open', bursary: bursaryDetail });
    });

    it('bySlug_410WithBursaryExtension_mapsToClosedWithSummary', () => {
      let result: BursaryDetailResult | undefined;
      api.bySlug(closedBursary.slug).subscribe((value) => (result = value));

      http.expectOne(`/api/v1/bursaries/${closedBursary.slug}`).flush(
        {
          title: 'This bursary has closed.',
          status: 410,
          errors: [{ code: 'Bursary.Closed', message: 'This bursary has closed.' }],
          bursary: closedBursary,
        },
        { status: 410, statusText: 'Gone' },
      );
      expect(result).toEqual({ kind: 'closed', summary: closedBursary });
    });

    it('bySlug_410WithoutBursaryExtension_rethrowsApiError', () => {
      let error: unknown;
      api.bySlug('gone').subscribe({ error: (e: unknown) => (error = e) });

      http.expectOne('/api/v1/bursaries/gone').flush(
        { title: 'Gone', errors: [{ code: 'Bursary.Gone', message: 'Gone' }] },
        { status: 410, statusText: 'Gone' },
      );
      expect(error).toBeInstanceOf(ApiError);
      expect((error as ApiError).status).toBe(410);
    });

    it('bySlug_404_rethrowsApiErrorWithCode', () => {
      let error: unknown;
      api.bySlug('missing').subscribe({ error: (e: unknown) => (error = e) });

      http.expectOne('/api/v1/bursaries/missing').flush(
        { title: 'Bursary not found', errors: [{ code: 'Bursary.NotFound', message: 'Not found' }] },
        { status: 404, statusText: 'Not Found' },
      );
      expect(error).toMatchObject({ status: 404, code: 'Bursary.NotFound' });
    });

    it('bySlug_encodesSlugInPath', () => {
      api.bySlug('a/b c').subscribe({ error: () => undefined });

      const req = http.expectOne('/api/v1/bursaries/a%2Fb%20c');
      req.flush(null, { status: 404, statusText: 'Not Found' });
    });
  });
});
