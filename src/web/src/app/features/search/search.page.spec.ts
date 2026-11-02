import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { BursarySummaryResponse, PagedResponse } from '../../core/api/models';
import { bursarySummaries, fields, institutions, openBursary } from '../../../testing/fixtures';
import { routes } from '../../app.routes';

const SEARCH = '/api/v1/bursaries/search';

describe('SearchPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let router: Router;
  let el: HTMLElement;

  async function open(url: string) {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    harness.detectChanges();
    http.expectOne('/api/v1/taxonomy/institutions').flush(institutions);
    http.expectOne('/api/v1/taxonomy/fields').flush(fields);
    el = harness.routeNativeElement as HTMLElement;
  }

  async function settle() {
    await harness.fixture.whenStable();
    harness.detectChanges();
  }

  function page(items: BursarySummaryResponse[], extra: Partial<PagedResponse<BursarySummaryResponse>> = {}) {
    return { items, page: 1, pageSize: 20, totalCount: items.length, ...extra };
  }

  function manyBursaries(n: number): BursarySummaryResponse[] {
    return Array.from({ length: n }, (_, i) => ({ ...openBursary, id: `id-${i}`, slug: `slug-${i}` }));
  }

  afterEach(() => http.verify());

  it('load_pending_showsSkeletons', async () => {
    await open('/search');
    expect(el.querySelectorAll('app-skeleton-card').length).toBe(3);
    http.expectOne((r) => r.url === SEARCH).flush(page([]));
  });

  it('results_loaded_showCountAndCards', async () => {
    await open('/search');
    http.expectOne((r) => r.url === SEARCH).flush(page(bursarySummaries, { totalCount: 23 }));
    await settle();
    expect(el.textContent).toContain('23 bursaries match');
    expect(el.querySelectorAll('app-bursary-card').length).toBe(3);
    expect(el.querySelectorAll('app-skeleton-card').length).toBe(0);
  });

  it('criteriaChange_cancelsInFlightRequestAndSearchesAgain', async () => {
    await open('/search');
    const first = http.expectOne((r) => r.url === SEARCH);
    await router.navigateByUrl('/search?studyLevel=Honours');
    await settle();
    expect(first.cancelled).toBe(true);
    const second = http.expectOne((r) => r.url === SEARCH);
    expect(second.request.params.get('studyLevel')).toBe('Honours');
    second.flush(page([openBursary]));
  });

  it('empty_showsMessageAndClearFiltersResetsUrl', async () => {
    await open('/search?average=40&includeClosed=true');
    http.expectOne((r) => r.url === SEARCH).flush(page([]));
    await settle();
    expect(el.textContent).toContain('No bursaries match');
    Array.from(el.querySelectorAll('app-empty-state button'))
      .find((b) => b.textContent?.includes('Clear filters'))!
      .dispatchEvent(new Event('click'));
    await settle();
    expect(router.url).toBe('/search');
    http.expectOne((r) => r.url === SEARCH).flush(page([]));
  });

  it('error_retryCallsApiAgain', async () => {
    await open('/search');
    http.expectOne((r) => r.url === SEARCH).flush({ title: 'boom' }, { status: 500, statusText: 'Server Error' });
    await settle();
    expect(el.querySelector('app-error-state')).not.toBeNull();
    el.querySelector('app-error-state button')!.dispatchEvent(new Event('click'));
    await settle();
    http.expectOne((r) => r.url === SEARCH).flush(page([openBursary]));
    await settle();
    expect(el.querySelector('app-error-state')).toBeNull();
    expect(el.querySelectorAll('app-bursary-card').length).toBe(1);
  });

  it('adSlot_afterEverySixthCard_isHidden', async () => {
    await open('/search');
    http.expectOne((r) => r.url === SEARCH).flush(page(manyBursaries(13)));
    await settle();
    const slots = el.querySelectorAll<HTMLElement>('.ad-slot');
    expect(slots.length).toBe(2);
    expect(slots[0].hidden).toBe(true);
    expect(slots[0].previousElementSibling!.querySelector('app-bursary-card')).not.toBeNull();
    expect(slots[0].previousElementSibling!.previousElementSibling).not.toBeNull();
    expect(el.querySelectorAll('li').length).toBe(15);
    expect(el.querySelectorAll('li')[6].classList.contains('ad-slot')).toBe(true);
  });

  it('noAdSlot_fewerThanSixCards', async () => {
    await open('/search');
    http.expectOne((r) => r.url === SEARCH).flush(page(manyBursaries(5)));
    await settle();
    expect(el.querySelectorAll('.ad-slot').length).toBe(0);
  });

  it('paging_nextAndPrevious_updateUrl', async () => {
    await open('/search');
    http.expectOne((r) => r.url === SEARCH).flush(page(manyBursaries(20), { totalCount: 45 }));
    await settle();
    expect(el.textContent).toContain('Page 1 of 3');
    expect(el.querySelector<HTMLButtonElement>('.prev')!.disabled).toBe(true);
    el.querySelector<HTMLButtonElement>('.next')!.click();
    await settle();
    expect(router.url).toBe('/search?page=2');
    http.expectOne((r) => r.url === SEARCH).flush(page(manyBursaries(20), { page: 2, totalCount: 45 }));
    await settle();
    expect(el.textContent).toContain('Page 2 of 3');
    el.querySelector<HTMLButtonElement>('.prev')!.click();
    await settle();
    expect(router.url).toBe('/search');
    http.expectOne((r) => r.url === SEARCH).flush(page(manyBursaries(20), { totalCount: 45 }));
  });

  it('paging_singlePage_hidesPagination', async () => {
    await open('/search');
    http.expectOne((r) => r.url === SEARCH).flush(page([openBursary]));
    await settle();
    expect(el.querySelector('app-pagination nav')).toBeNull();
  });
});
