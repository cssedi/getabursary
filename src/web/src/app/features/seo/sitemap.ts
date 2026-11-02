// Pure builders for /robots.txt and /sitemap.xml. No Angular or Express imports, so they unit-test easily
// and `server.ts` stays a thin wrapper.

export const DEFAULT_PUBLIC_BASE_URL = 'https://getabursary.co.za';
const PAGE_SIZE = 50;
const MAX_PAGES = 200;

interface FieldNode {
  slug: string;
  children?: FieldNode[];
}

interface SearchPage {
  items: { slug: string }[];
  totalCount: number;
}

export type FetchLike = (url: string, init?: { signal?: AbortSignal }) => Promise<{
  ok: boolean;
  status: number;
  json(): Promise<unknown>;
}>;

export function normaliseBaseUrl(baseUrl: string | undefined): string {
  const trimmed = (baseUrl ?? '').trim().replace(/\/+$/, '');
  return trimmed || DEFAULT_PUBLIC_BASE_URL;
}

export function buildRobotsTxt(baseUrl: string): string {
  const base = normaliseBaseUrl(baseUrl);
  return ['User-agent: *', 'Allow: /', 'Disallow: /search', '', `Sitemap: ${base}/sitemap.xml`, ''].join('\n');
}

export function escapeXml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

export function buildSitemapXml(baseUrl: string, fieldSlugs: string[], bursarySlugs: string[]): string {
  const base = normaliseBaseUrl(baseUrl);
  const paths = [
    '/',
    '/about',
    ...fieldSlugs.map((s) => `/bursaries/field/${encodeURIComponent(s)}`),
    ...bursarySlugs.map((s) => `/bursaries/${encodeURIComponent(s)}`),
  ];
  const urls = [...new Set(paths)].map((p) => `  <url><loc>${escapeXml(base + p)}</loc></url>`);
  return [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
    ...urls,
    '</urlset>',
    '',
  ].join('\n');
}

function flattenFieldSlugs(fields: FieldNode[]): string[] {
  return fields.flatMap((f) => [f.slug, ...flattenFieldSlugs(f.children ?? [])]);
}

async function getJson<T>(fetchFn: FetchLike, url: string): Promise<T> {
  const response = await fetchFn(url, { signal: AbortSignal.timeout(10_000) });
  if (!response.ok) {
    throw new Error(`API ${response.status} for ${url}`);
  }
  return (await response.json()) as T;
}

/** Loads field slugs and every open bursary slug from the API. Throws if any call fails. */
export async function loadSitemapSlugs(
  apiBaseUrl: string,
  fetchFn: FetchLike,
): Promise<{ fieldSlugs: string[]; bursarySlugs: string[] }> {
  const api = apiBaseUrl.replace(/\/+$/, '');
  const fields = await getJson<FieldNode[]>(fetchFn, `${api}/api/v1/taxonomy/fields`);
  const bursarySlugs: string[] = [];
  for (let page = 1; page <= MAX_PAGES; page++) {
    const result = await getJson<SearchPage>(
      fetchFn,
      `${api}/api/v1/bursaries/search?pageSize=${PAGE_SIZE}&page=${page}`,
    );
    bursarySlugs.push(...result.items.map((b) => b.slug));
    if (result.items.length === 0 || page * PAGE_SIZE >= result.totalCount) break;
  }
  return { fieldSlugs: flattenFieldSlugs(fields), bursarySlugs };
}

export interface SitemapCache {
  get(now: number): Promise<string>;
}

export interface SitemapResponse {
  status: 200 | 503;
  contentType: 'application/xml' | 'text/plain';
  body: string;
}

/** Turns the cache result into a response: 503 (never an empty sitemap) when the API fails. */
export async function respondWithSitemap(cache: SitemapCache, now: number): Promise<SitemapResponse> {
  try {
    return { status: 200, contentType: 'application/xml', body: await cache.get(now) };
  } catch (error) {
    console.error('sitemap.xml failed', error);
    return { status: 503, contentType: 'text/plain', body: 'Sitemap temporarily unavailable' };
  }
}

/** Builds the sitemap on demand and keeps a successful result for `ttlMs`. Failures are never cached. */
export function createSitemapCache(
  build: () => Promise<string>,
  ttlMs = 60 * 60 * 1000,
): SitemapCache {
  let cached: { xml: string; expires: number } | undefined;
  let inFlight: Promise<string> | undefined;
  return {
    async get(now: number): Promise<string> {
      if (cached && now < cached.expires) return cached.xml;
      // Concurrent callers share one build; cleared afterwards so failures are not cached.
      inFlight ??= build()
        .then((xml) => {
          cached = { xml, expires: now + ttlMs };
          return xml;
        })
        .finally(() => (inFlight = undefined));
      return inFlight;
    },
  };
}
