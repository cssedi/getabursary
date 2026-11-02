import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { bursaryDetail, closedBursary, openBursary } from '../../../testing/fixtures';
import { routes } from '../../app.routes';
import { errorInterceptor } from '../../core/api/error.interceptor';

const SLUG = openBursary.slug;
const DETAIL_URL = `/api/v1/bursaries/${SLUG}`;

describe('BursaryDetailPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let el: HTMLElement;

  /**
   * Starts navigation and returns it unawaited: the resolver waits for the API, so the navigation
   * settles only after the test answers the request.
   */
  async function open(path = `/bursaries/${SLUG}`) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
    return { navigation: harness.navigateByUrl(path) };
  }

  /** The router starts the resolver asynchronously (lazy-loaded route), so poll until the request is issued. */
  async function nextRequest(url: string): Promise<TestRequest> {
    for (let attempt = 0; attempt < 100; attempt++) {
      const [request] = http.match(url);
      if (request) return request;
      await new Promise((resolve) => setTimeout(resolve, 0));
    }
    throw new Error(`No request was made to ${url}`);
  }

  async function settle() {
    await harness.fixture.whenStable();
    harness.detectChanges();
    el = harness.routeNativeElement as HTMLElement;
  }

  function headings(): string[] {
    return Array.from(el.querySelectorAll('h2')).map((h) => h.textContent?.trim() ?? '');
  }

  afterEach(() => {
    http.verify();
    document.head
      .querySelectorAll('meta[name="robots"], script[data-seo-jsonld], link[rel="canonical"]')
      .forEach((n) => n.remove());
  });

  it('open_loaded_rendersEverySection', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush(bursaryDetail);
    await navigation;
    await settle();

    expect(el.querySelector('h1')?.textContent).toBe(bursaryDetail.name);
    expect(el.textContent).toContain(bursaryDetail.funderName!);
    expect(el.querySelector('app-closing-date-badge')).not.toBeNull();
    expect(el.querySelectorAll('app-source-icons a').length).toBe(2);
    expect(headings()).toEqual([
      'Who can apply',
      'Fields of study and qualifications',
      'What’s covered',
      'Work-back obligation',
      'About this bursary',
    ]);
    expect(el.textContent).toContain(bursaryDetail.summary);
    expect(el.textContent).toContain('Mathematics: 70%');
    expect(el.textContent).toContain('Physical Sciences: 60%');
    expect(el.textContent).toContain('South African citizens only');
    expect(el.textContent).toContain('Any province');
    expect(el.textContent).toContain('Engineering');
    expect(el.textContent).toContain('BSc (Eng) Electrical Engineering (UCT)');
    expect(el.textContent).toContain('Tuition');
    expect(el.textContent).toContain(bursaryDetail.valueSummary!);
    expect(el.textContent).toContain(bursaryDetail.workBackDetails!);
    expect(el.textContent).toContain('Last updated 20 Oct 2026');
  });

  it('open_applyLink_opensFunderSiteInNewTabWithNofollow', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush(bursaryDetail);
    await navigation;
    await settle();

    const apply = el.querySelector<HTMLAnchorElement>('.apply-bar a')!;
    expect(apply.textContent?.trim()).toBe(`Apply on ${bursaryDetail.funderName}’s site`);
    expect(apply.getAttribute('href')).toBe(bursaryDetail.applyUrl);
    expect(apply.getAttribute('target')).toBe('_blank');
    const rel = apply.getAttribute('rel')!.split(' ');
    expect(rel).toContain('noopener');
    expect(rel).toContain('nofollow');
  });

  it('open_noFunderName_applyLabelUsesGenericFallback', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush({ ...bursaryDetail, funderName: null });
    await navigation;
    await settle();

    expect(el.querySelector('.apply-bar a')?.textContent?.trim()).toBe(
      'Apply on the funder’s site',
    );
  });

  it('open_applyUrl_rendersStickyApplyBarAndReservesPageSpace', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush(bursaryDetail);
    await navigation;
    await settle();

    // The 768 px media query lives in the component styles; here we check the structure it targets.
    expect(el.querySelector('.detail')!.classList).toContain('with-apply-bar');
    const bar = el.querySelector('.apply-bar')!;
    expect(bar.parentElement).toBe(el.querySelector('.detail'));
    expect(bar.querySelector('a.apply-button')).not.toBeNull();
  });

  it('open_noApplyUrl_rendersNoApplyBar', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush({ ...bursaryDetail, applyUrl: null });
    await navigation;
    await settle();

    expect(el.querySelector('.apply-bar')).toBeNull();
    expect(el.querySelector('.detail')!.classList).not.toContain('with-apply-bar');
  });

  it('open_adSlot_isHidden', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush(bursaryDetail);
    await navigation;
    await settle();

    expect(el.querySelector<HTMLElement>('.ad-slot')!.hidden).toBe(true);
  });

  it('open_noWorkBack_hidesWorkBackSection', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush({
      ...bursaryDetail,
      workBackRequired: false,
      workBackDetails: null,
    });
    await navigation;
    await settle();

    expect(headings()).not.toContain('Work-back obligation');
  });

  it('open_longSummary_truncatesMetaDescriptionTo155Characters', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush({
      ...bursaryDetail,
      summary: 'Funds study. '.repeat(30),
    });
    await navigation;
    await settle();

    const description = document.head
      .querySelector('meta[name="description"]')!
      .getAttribute('content')!;
    expect(description.length).toBeLessThanOrEqual(155);
    expect(description.endsWith('…')).toBe(true);
  });

  it('open_loaded_setsSeoTitleCanonicalAndStructuredData', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush(bursaryDetail);
    await navigation;
    await settle();

    expect(document.title).toBe(`${bursaryDetail.name} | GetABursary`);
    expect(document.head.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe(
      `https://getabursary.co.za/bursaries/${SLUG}`,
    );
    expect(document.head.querySelector('meta[name="robots"]')).toBeNull();
    const jsonLd = JSON.parse(document.head.querySelector('script[data-seo-jsonld]')!.textContent!);
    expect(jsonLd['@type']).toBe('MonetaryGrant');
    expect(jsonLd.name).toBe(bursaryDetail.name);
    expect(jsonLd.funder.name).toBe(bursaryDetail.funderName);
    expect(jsonLd.applicationDeadline).toBe(bursaryDetail.closingDate);
  });

  it('open_statusClosed_hidesSaveButton', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush({ ...bursaryDetail, status: 'Closed' });
    await navigation;
    await settle();

    expect(el.querySelector('h1')).not.toBeNull();
    expect(el.querySelector('app-save-button')).toBeNull();
  });

  it('closed_410_rendersSummaryOnlyWithNoindex', async () => {
    const { navigation } = await open(`/bursaries/${closedBursary.slug}`);
    (await nextRequest(`/api/v1/bursaries/${closedBursary.slug}`)).flush(
      { title: 'Bursary.Closed', bursary: closedBursary },
      { status: 410, statusText: 'Gone' },
    );
    await navigation;
    await settle();

    expect(el.querySelector('app-closed-bursary')).not.toBeNull();
    expect(el.querySelector('h1')?.textContent).toBe(closedBursary.name);
    expect(el.textContent).toContain('This bursary closed on 30 Sept 2026.');
    expect(el.querySelector('app-closing-date-badge')).not.toBeNull();
    expect(el.querySelectorAll('app-source-icons a').length).toBe(1);
    expect(headings()).toEqual([]);
    expect(el.textContent).not.toContain('Apply on');
    expect(el.querySelector('.apply-bar')).toBeNull();
    expect(el.querySelector('.ad-slot')).toBeNull();
    expect(document.head.querySelector('meta[name="robots"]')?.getAttribute('content')).toBe(
      'noindex',
    );
    expect(document.head.querySelector('script[data-seo-jsonld]')).toBeNull();
  });

  it('notFound_404_rendersNotFoundContent', async () => {
    const { navigation } = await open('/bursaries/no-such-bursary');
    (await nextRequest('/api/v1/bursaries/no-such-bursary')).flush(
      { title: 'Bursary.NotFound' },
      { status: 404, statusText: 'Not Found' },
    );
    await navigation;
    await settle();

    expect(el.textContent).toContain('Page not found');
    expect(el.querySelector('h1')?.textContent).not.toBe(bursaryDetail.name);
    expect(el.querySelector('app-error-state')).toBeNull();
  });

  it('error_500_showsRetryAndRetryReloadsBursary', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush(
      { title: 'boom' },
      { status: 500, statusText: 'Server Error' },
    );
    await navigation;
    await settle();

    expect(el.querySelector('app-error-state')).not.toBeNull();
    el.querySelector<HTMLButtonElement>('app-error-state button')!.click();
    (await nextRequest(DETAIL_URL)).flush(bursaryDetail);
    await settle();

    expect(el.querySelector('app-error-state')).toBeNull();
    expect(el.querySelector('h1')?.textContent).toBe(bursaryDetail.name);
  });

  it('navigation_toAnotherBursary_replacesSeoTags', async () => {
    const { navigation } = await open();
    (await nextRequest(DETAIL_URL)).flush(bursaryDetail);
    await navigation;
    await settle();

    const next = harness.navigateByUrl(`/bursaries/${closedBursary.slug}`);
    (await nextRequest(`/api/v1/bursaries/${closedBursary.slug}`)).flush(
      { title: 'Bursary.Closed', bursary: closedBursary },
      { status: 410, statusText: 'Gone' },
    );
    await next;
    await settle();

    expect(document.head.querySelectorAll('meta[name="robots"]').length).toBe(1);
    expect(document.head.querySelectorAll('script[data-seo-jsonld]').length).toBe(0);
    expect(document.head.querySelectorAll('link[rel="canonical"]').length).toBe(1);
    expect(document.title).toBe(`${closedBursary.name} | GetABursary`);
  });
});
