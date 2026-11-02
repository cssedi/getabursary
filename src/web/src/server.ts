import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { join } from 'node:path';
import {
  buildRobotsTxt,
  buildSitemapXml,
  createSitemapCache,
  loadSitemapSlugs,
  normaliseBaseUrl,
  respondWithSitemap,
} from './app/features/seo/sitemap';

const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine();

/**
 * Example Express Rest API endpoints can be defined here.
 * Uncomment and define endpoints as necessary.
 *
 * Example:
 * ```ts
 * app.get('/api/{*splat}', (req, res) => {
 *   // Handle API request
 * });
 * ```
 */

const publicBaseUrl = normaliseBaseUrl(process.env['PUBLIC_BASE_URL']);
const apiInternalUrl = process.env['API_INTERNAL_URL'] ?? 'http://localhost:5266';
const sitemapCache = createSitemapCache(async () => {
  const { fieldSlugs, bursarySlugs } = await loadSitemapSlugs(apiInternalUrl, fetch);
  return buildSitemapXml(publicBaseUrl, fieldSlugs, bursarySlugs);
});

app.get('/robots.txt', (_req, res) => {
  res.type('text/plain').send(buildRobotsTxt(publicBaseUrl));
});

app.get('/sitemap.xml', async (_req, res) => {
  const { status, contentType, body } = await respondWithSitemap(sitemapCache, Date.now());
  if (status === 503) res.set('Retry-After', '300');
  res.status(status).type(contentType).send(body);
});

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 */
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) =>
      response ? writeResponseToNodeResponse(response, res) : next(),
    )
    .catch(next);
});

/**
 * Start the server if this module is the main entry point, or it is ran via PM2.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, (error) => {
    if (error) {
      throw error;
    }

    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
