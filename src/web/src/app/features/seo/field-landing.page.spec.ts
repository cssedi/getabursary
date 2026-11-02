import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RESPONSE_INIT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { bursarySummaries, fields } from '../../../testing/fixtures';

const SEARCH = '/api/v1/bursaries/search';

describe('FieldLandingPage', () => {
  let http: HttpTestingController;
  let harness: RouterTestingHarness;
  const responseInit: ResponseInit = {};

  async function open(url: string) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RESPONSE_INIT, useValue: responseInit },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    harness.detectChanges();
  }

  async function settle() {
    await harness.fixture.whenStable();
    harness.detectChanges();
  }

  afterEach(() => http.verify());

  it('render_knownSlug_showsHeadingAndSearchesByFieldId', async () => {
    await open('/bursaries/field/engineering');
    http.expectOne('/api/v1/taxonomy/fields').flush(fields);
    await settle();
    const req = http.expectOne((r) => r.url === SEARCH);
    expect(req.request.params.getAll('fieldIds')).toEqual([fields[0].id]);
    expect(req.request.params.get('sort')).toBe('ClosingSoonest');
    req.flush({ items: bursarySummaries, page: 1, pageSize: 20, totalCount: 3 });
    await settle();

    const el = harness.routeNativeElement as HTMLElement;
    expect(el.querySelector('h1')?.textContent).toContain('Engineering bursaries in South Africa');
    expect(el.querySelectorAll('app-bursary-card').length).toBe(3);
    const refine = el.querySelector<HTMLAnchorElement>('a[href^="/search"]')!;
    expect(refine.textContent?.trim()).toBe('Refine by your degree');
    expect(refine.getAttribute('href')).toBe(`/search?fieldIds=${fields[0].id}`);
    expect(document.title).toBe(`Engineering bursaries ${new Date().getFullYear()} | GetABursary`);
    expect(document.head.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe(
      'https://getabursary.co.za/bursaries/field/engineering',
    );
    expect(responseInit.status).toBeUndefined();
  });

  it('render_childFieldSlug_resolvesNestedField', async () => {
    await open('/bursaries/field/chemical-engineering');
    http.expectOne('/api/v1/taxonomy/fields').flush(fields);
    await settle();
    const req = http.expectOne((r) => r.url === SEARCH);
    expect(req.request.params.getAll('fieldIds')).toEqual([fields[0].children[0].id]);
    req.flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });
  });

  it('render_unknownSlug_showsNotFoundWith404AndNoSearch', async () => {
    await open('/bursaries/field/no-such-field');
    http.expectOne('/api/v1/taxonomy/fields').flush(fields);
    await settle();
    const el = harness.routeNativeElement as HTMLElement;
    expect(el.querySelector('app-not-found-page')).not.toBeNull();
    expect(responseInit.status).toBe(404);
    http.expectNone((r) => r.url === SEARCH);
  });

  it('render_taxonomyFails_showsErrorState', async () => {
    await open('/bursaries/field/engineering');
    http.expectOne('/api/v1/taxonomy/fields').flush({ title: 'boom' }, { status: 500, statusText: 'Server Error' });
    await settle();
    expect((harness.routeNativeElement as HTMLElement).querySelector('app-error-state')).not.toBeNull();
  });
});
