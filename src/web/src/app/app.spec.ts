import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { App } from './app';
import { routes } from './app.routes';
import { API_BASE_URL } from './core/api/api-base-url';
import { bursaryDetail } from '../testing/fixtures';

describe('App shell', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App], providers: [provideRouter(routes)] }).compileComponents();
  });

  it('renders header logo and footer links', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const logo = el.querySelector('app-header a');
    expect(logo?.textContent).toContain('GetABursary');
    expect(logo?.getAttribute('href')).toBe('/');
    const hrefs = Array.from(el.querySelectorAll('app-footer a')).map((a) => a.getAttribute('href'));
    expect(hrefs).toEqual(['/about', '/privacy']);
    expect(el.querySelector('app-footer')?.textContent).toContain(
      'Bursary info is gathered from public sources. Always confirm details on the funder',
    );
  });

  it('skipLink_isFirstFocusable_andFocusesContent', async () => {
    const fixture = TestBed.createComponent(App);
    document.body.appendChild(fixture.nativeElement);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const first = el.querySelector('a, button, input, select, [tabindex]');
    expect(first?.textContent).toContain('Skip to content');
    const main = el.querySelector<HTMLElement>('main#content');
    expect(main?.getAttribute('tabindex')).toBe('-1');
    (first as HTMLElement).click();
    expect(document.activeElement).toBe(main);
    fixture.nativeElement.remove();
  });

  it('provides an empty API base URL in the browser', () => {
    expect(TestBed.inject(API_BASE_URL)).toBe('');
  });
});

describe('Routes', () => {
  const cases: [string, string][] = [
    ['/', 'Find bursaries for'],
    ['/search', 'Search bursaries'],
    ['/about', 'AboutPage'],
    ['/privacy', 'PrivacyPage'],
    ['/does/not/exist', 'NotFoundPage'],
  ];

  it('/bursaries/:slug resolves the bursary before rendering BursaryDetailPage', async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    const http = TestBed.inject(HttpTestingController);
    const harness = await RouterTestingHarness.create();
    // The resolver waits for the API, so the navigation settles only after the request is answered.
    const navigation = harness.navigateByUrl('/bursaries/anglo-american-engineering-bursary-2027');
    const request = await vi.waitFor(() => http.expectOne('/api/v1/bursaries/anglo-american-engineering-bursary-2027'));
    request.flush(bursaryDetail);
    await navigation;
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe(bursaryDetail.name);
  });

  for (const [url, name] of cases) {
    it(`${url} resolves to ${name}`, async () => {
      TestBed.configureTestingModule({
        providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
      });
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl(url);
      expect(harness.routeNativeElement?.textContent).toContain(name);
    });
  }
});
