import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { App } from './app';
import { routes } from './app.routes';
import { API_BASE_URL } from './core/api/api-base-url';

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

  it('provides an empty API base URL in the browser', () => {
    expect(TestBed.inject(API_BASE_URL)).toBe('');
  });
});

describe('Routes', () => {
  const cases: [string, string][] = [
    ['/', 'HomePage'],
    ['/search', 'SearchPage'],
    ['/bursaries/some-slug', 'BursaryDetailPage'],
    ['/bursaries/field/engineering', 'FieldLandingPage'],
    ['/about', 'AboutPage'],
    ['/privacy', 'PrivacyPage'],
    ['/does/not/exist', 'NotFoundPage'],
  ];

  for (const [url, name] of cases) {
    it(`${url} resolves to ${name}`, async () => {
      TestBed.configureTestingModule({ providers: [provideRouter(routes)] });
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl(url);
      expect(harness.routeNativeElement?.textContent).toContain(name);
    });
  }
});
