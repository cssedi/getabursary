import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { SITE_ORIGIN, SeoService } from './seo.service';

describe('SeoService', () => {
  let service: SeoService;
  let doc: Document;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SeoService);
    doc = TestBed.inject(DOCUMENT);
  });

  afterEach(() => {
    doc.head
      .querySelectorAll('meta[name="robots"], script[data-seo-jsonld]')
      .forEach((el) => el.remove());
  });

  const metaContent = (selector: string) =>
    doc.head.querySelector(selector)?.getAttribute('content');

  it('set_fullConfig_setsTitleDescriptionCanonicalAndOpenGraph', () => {
    service.set({
      title: 'Anglo Bursary',
      description: 'Funds tuition.',
      canonicalPath: '/bursaries/anglo',
    });

    expect(doc.title).toBe('Anglo Bursary | GetABursary');
    expect(metaContent('meta[name="description"]')).toBe('Funds tuition.');
    expect(doc.head.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe(
      `${SITE_ORIGIN}/bursaries/anglo`,
    );
    expect(metaContent('meta[property="og:title"]')).toBe('Anglo Bursary | GetABursary');
    expect(metaContent('meta[property="og:description"]')).toBe('Funds tuition.');
    expect(metaContent('meta[property="og:url"]')).toBe(`${SITE_ORIGIN}/bursaries/anglo`);
  });

  it('set_calledTwice_replacesTagsWithoutDuplicates', () => {
    service.set({
      title: 'First',
      description: 'One',
      canonicalPath: '/bursaries/first',
      jsonLd: { '@type': 'MonetaryGrant', name: 'First' },
    });
    service.set({
      title: 'Second',
      description: 'Two',
      canonicalPath: '/bursaries/second',
      jsonLd: { '@type': 'MonetaryGrant', name: 'Second' },
    });

    expect(doc.querySelectorAll('meta[name="description"]').length).toBe(1);
    expect(doc.querySelectorAll('link[rel="canonical"]').length).toBe(1);
    expect(doc.querySelectorAll('meta[property="og:title"]').length).toBe(1);
    expect(doc.querySelectorAll('meta[property="og:url"]').length).toBe(1);
    expect(doc.querySelectorAll('script[data-seo-jsonld]').length).toBe(1);

    expect(doc.title).toBe('Second | GetABursary');
    expect(metaContent('meta[name="description"]')).toBe('Two');
    expect(doc.head.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe(
      `${SITE_ORIGIN}/bursaries/second`,
    );
    expect(JSON.parse(doc.head.querySelector('script[data-seo-jsonld]')!.textContent!).name).toBe(
      'Second',
    );
  });

  it('set_withoutJsonLd_removesPreviousStructuredData', () => {
    service.set({ title: 'A', description: 'a', canonicalPath: '/a', jsonLd: { name: 'A' } });
    service.set({ title: 'B', description: 'b', canonicalPath: '/b' });

    expect(doc.querySelectorAll('script[data-seo-jsonld]').length).toBe(0);
  });

  it('set_jsonLdWithScriptClosingTag_escapesAngleBrackets', () => {
    service.set({
      title: 'A',
      description: 'a',
      canonicalPath: '/a',
      jsonLd: { name: '</script>' },
    });

    const script = doc.head.querySelector('script[data-seo-jsonld]')!;
    expect(script.textContent).not.toContain('</script>');
    expect(JSON.parse(script.textContent!).name).toBe('</script>');
  });

  it('set_robotsNoindex_addsRobotsMeta', () => {
    service.set({ title: 'Closed', description: 'c', canonicalPath: '/c', robots: 'noindex' });

    expect(metaContent('meta[name="robots"]')).toBe('noindex');
  });

  it('set_noRobotsAfterNoindex_removesRobotsMeta', () => {
    service.set({ title: 'Closed', description: 'c', canonicalPath: '/c', robots: 'noindex' });
    service.set({ title: 'Open', description: 'o', canonicalPath: '/o' });

    expect(doc.querySelectorAll('meta[name="robots"]').length).toBe(0);
  });
});
