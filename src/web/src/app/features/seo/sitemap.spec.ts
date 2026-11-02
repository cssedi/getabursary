import {
  FetchLike,
  buildRobotsTxt,
  buildSitemapXml,
  createSitemapCache,
  loadSitemapSlugs,
  respondWithSitemap,
} from './sitemap';

const fieldTree = [
  { slug: 'engineering', children: [{ slug: 'chemical-engineering', children: [] }] },
  { slug: 'law', children: [] },
];

function fakeFetch(totalCount: number, failOn?: string): FetchLike {
  return async (url) => {
    if (failOn && url.includes(failOn)) return { ok: false, status: 500, json: async () => ({}) };
    if (url.endsWith('/api/v1/taxonomy/fields')) return { ok: true, status: 200, json: async () => fieldTree };
    const page = Number(new URL(url).searchParams.get('page'));
    const start = (page - 1) * 50;
    const items = Array.from({ length: Math.max(0, Math.min(50, totalCount - start)) }, (_, i) => ({
      slug: `bursary-${start + i}`,
    }));
    return { ok: true, status: 200, json: async () => ({ items, totalCount }) };
  };
}

describe('buildRobotsTxt', () => {
  it('buildRobotsTxt_default_allowsAllDisallowsSearchAndListsSitemap', () => {
    expect(buildRobotsTxt('https://getabursary.co.za')).toBe(
      'User-agent: *\nAllow: /\nDisallow: /search\n\nSitemap: https://getabursary.co.za/sitemap.xml\n',
    );
  });

  it('buildRobotsTxt_customBaseWithTrailingSlash_usesIt', () => {
    expect(buildRobotsTxt('https://staging.example.org/')).toContain(
      'Sitemap: https://staging.example.org/sitemap.xml',
    );
  });

  it('buildRobotsTxt_emptyBase_fallsBackToDefault', () => {
    expect(buildRobotsTxt('')).toContain('Sitemap: https://getabursary.co.za/sitemap.xml');
  });
});

describe('buildSitemapXml', () => {
  it('buildSitemapXml_fixtureData_listsStaticFieldAndBursaryUrls', () => {
    const xml = buildSitemapXml('https://getabursary.co.za', ['engineering', 'law'], ['anglo-2027']);
    expect(xml.startsWith('<?xml version="1.0" encoding="UTF-8"?>')).toBe(true);
    expect(xml).toContain('<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">');
    for (const loc of [
      'https://getabursary.co.za/',
      'https://getabursary.co.za/about',
      'https://getabursary.co.za/bursaries/field/engineering',
      'https://getabursary.co.za/bursaries/field/law',
      'https://getabursary.co.za/bursaries/anglo-2027',
    ]) {
      expect(xml).toContain(`<loc>${loc}</loc>`);
    }
    const parsed = new DOMParser().parseFromString(xml, 'application/xml');
    expect(parsed.querySelector('parsererror')).toBeNull();
    expect(parsed.getElementsByTagName('url').length).toBe(5);
  });

  it('buildSitemapXml_specialCharacters_areEscapedAndEncoded', () => {
    const xml = buildSitemapXml('https://getabursary.co.za', [], ['a&b', "x'y"]);
    const parsed = new DOMParser().parseFromString(xml, 'application/xml');
    expect(parsed.querySelector('parsererror')).toBeNull();
    expect(xml).toContain('/bursaries/a%26b');
  });
});

describe('loadSitemapSlugs', () => {
  it('loadSitemapSlugs_multiplePages_collectsAllSlugsAndFlattensFields', async () => {
    const result = await loadSitemapSlugs('http://api:8080/', fakeFetch(120));
    expect(result.bursarySlugs.length).toBe(120);
    expect(result.fieldSlugs).toEqual(['engineering', 'chemical-engineering', 'law']);
  });

  it('loadSitemapSlugs_searchFails_rejects', async () => {
    await expect(loadSitemapSlugs('http://api', fakeFetch(10, '/bursaries/search'))).rejects.toThrow();
  });

  it('loadSitemapSlugs_fieldsFail_rejects', async () => {
    await expect(loadSitemapSlugs('http://api', fakeFetch(10, '/taxonomy/fields'))).rejects.toThrow();
  });
});

describe('sitemap cache and response', () => {
  it('get_withinTtl_buildsOnce', async () => {
    const build = vi.fn().mockResolvedValue('<xml/>');
    const cache = createSitemapCache(build, 1000);
    await cache.get(0);
    await cache.get(999);
    expect(build).toHaveBeenCalledTimes(1);
    await cache.get(1000);
    expect(build).toHaveBeenCalledTimes(2);
  });

  it('respondWithSitemap_apiFailure_returns503AndDoesNotCache', async () => {
    const build = vi.fn().mockRejectedValueOnce(new Error('down')).mockResolvedValue('<xml/>');
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const cache = createSitemapCache(build);
    const failed = await respondWithSitemap(cache, 0);
    expect(failed.status).toBe(503);
    expect(failed.body).not.toContain('<urlset');
    const ok = await respondWithSitemap(cache, 1);
    expect(ok).toEqual({ status: 200, contentType: 'application/xml', body: '<xml/>' });
  });
});
