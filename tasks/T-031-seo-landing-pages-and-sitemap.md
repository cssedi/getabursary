---
id: T-031
title: Field-of-study landing pages, sitemap.xml and robots.txt
phase: 4
area: web
model: sonnet
status: todo
depends_on: [T-028, T-029]
requirements: [NFR-02]
issue: 40
---

## Goal
Search engines can find and index GetABursary: every field of study gets a landing page with its open bursaries, and the site serves `sitemap.xml` and `robots.txt`.

## Context
- `AGENTS.md`, `specs/05-web.md` §2 (SSR base URL), §3 (`/bursaries/field/:fieldSlug`), §5 (`features/seo`), §7 (SEO)
- `src/web/src/app/features/search/**` (reuse `ResultListComponent`), `src/web/src/app/features/seo/seo.service.ts`
- API: `GET /api/v1/taxonomy/fields`, `GET /api/v1/bursaries/search` (spec 01 §6)

## Scope
You may create or modify **only**:
- `src/web/src/app/features/seo/**` and the field landing route entry
- `src/web/src/server.ts` (add `/sitemap.xml` and `/robots.txt` handlers only)
- `src/web/src/app/features/search/result-list/**` (only if an input is needed for reuse)

## Requirements
1. `FieldLandingPage` at `/bursaries/field/:fieldSlug`: resolves the slug via `TaxonomyApi.fields()` (unknown → 404 page); `<h1>` "{Field name} bursaries in South Africa"; a short intro; open bursaries for that field (search with `fieldIds=[id]`, `sort=ClosingSoonest`) using `ResultListComponent`; a link "Refine by your degree →" to `/search?fieldIds=...`. `SeoService` sets title "{Field} bursaries {current year} | GetABursary", description and canonical.
2. `GET /robots.txt`: allow all, `Disallow: /search` (thin, parameterised pages), and `Sitemap: https://getabursary.co.za/sitemap.xml` (base URL from the `PUBLIC_BASE_URL` env var, default `https://getabursary.co.za`).
3. `GET /sitemap.xml`: valid sitemap XML with `/`, `/about`, every field landing page, and every **open** bursary detail URL (page through `/api/v1/bursaries/search?pageSize=50` using `API_INTERNAL_URL`). Cache it in memory for 1 hour. If the API fails, return 503 (never an empty sitemap).
4. The home page links to the 6 most common field landing pages (a hard-coded list of slugs is fine; note it in the README).

## Acceptance criteria
- [ ] Tests: the landing page renders the heading and calls search with the field id; an unknown slug gives 404; the sitemap builder produces valid XML from fixture data (unit test the builder function separately from Express); robots output matches; an API failure returns 503
- [ ] `npm test -- --watch=false`, `npm run lint` and `npm run build` (within budgets) pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
