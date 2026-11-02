# GetABursary web (Angular SSR)

Angular (standalone, signals) with SSR, Tailwind CSS and Angular CDK. See `specs/05-web.md`.

## Run
```bash
cd src/web
npm ci
npm start            # http://localhost:4200, /api is proxied to http://localhost:5266
```
Run the API on port 5266 (`dotnet run --project src/GetABursary.Api`) for data.

## SEO
- `/robots.txt` and `/sitemap.xml` are served by `src/server.ts`. `PUBLIC_BASE_URL` (default `https://getabursary.co.za`) sets the absolute URLs. The sitemap reads the API at `API_INTERNAL_URL`, is cached for 1 hour, and returns 503 if the API fails.
- The home page links to six field landing pages from a hard-coded list in `src/app/features/seo/featured-fields.ts`. Slugs must match `data/seed/fields-of-study.json`.

## Test, lint, build
```bash
npm test -- --watch=false
npm run lint
npm run build        # production build, enforces size budgets
npm run serve:ssr    # serves the SSR build on :4000 (PORT to override; API_INTERNAL_URL for the API base)
```
