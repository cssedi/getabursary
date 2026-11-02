---
id: T-029
title: Bursary detail page with SSR, SEO metadata and closed state
phase: 4
area: web
model: haiku
status: todo
depends_on: [T-026]
requirements: [FR-06, FR-07, NFR-02]
issue: 38
---

## Goal
`/bursaries/:slug` server-renders a bursary's full structured eligibility with an "Apply on funder's site" button and complete SEO metadata. Closed bursaries show only their summary with a clear "closed" message.

## Context
- `AGENTS.md`, `specs/05-web.md` §3 (detail route), §5 (`features/bursary`, `features/seo` `SeoService`), §6 (apply button), §7 (SEO, a11y)
- `tasks/T-022-bursary-detail-endpoint.md` (response shape, 410 behaviour)
- `src/web/src/app/core/api/**`, `src/web/src/testing/fixtures.ts`

## Scope
You may create or modify **only**:
- `src/web/src/app/features/bursary/**` and the detail route entry
- `src/web/src/app/features/seo/seo.service.ts` (+ spec)
- `src/web/src/app/shared/ui/**` (reuse existing; add new components only if needed)

## Requirements
1. A route resolver (or `resource`) loads `BursaryApi.bySlug`. Unknown slug (404) → render `NotFoundPage` content with HTTP 404 during SSR.
2. Open bursary sections: header (name, funder, closing badge, source icons), "Who can apply" (study levels, minimum average, subject requirements as information only, citizenship, provinces), "Fields of study and qualifications" (field names; qualification names with institution short names), "What's covered" (coverage list, value summary), "Work-back obligation" (if any), "About" (`summary`), the big **Apply on funder's site** button (spec §6), "Last updated {date}", and a hidden `.ad-slot` placeholder.
3. Closed (`kind: 'closed'`): `ClosedBursaryComponent` shows the name, funder, "This bursary closed on {date}", source icons and a "Find open bursaries for similar fields" link to `/search?fieldIds=...` when field info exists. **No eligibility details.** During SSR, respond with HTTP 200 (the page is still useful) plus `<meta name="robots" content="noindex">`.
4. `SeoService.set({ title, description, canonicalPath, jsonLd? })`: sets `<title>` ("{name} | GetABursary"), meta description (summary truncated to 155 chars), canonical link, Open Graph title/description/url, and a JSON-LD `<script>` (schema.org `MonetaryGrant`: name, funder, url, `applicationDeadline` where possible). It replaces its tags rather than duplicating them on navigation.

## Acceptance criteria
- [ ] Tests: the open fixture renders each section and the apply link (`target="_blank"`, `rel` includes `noopener` and `nofollow`); the closed fixture renders the closed view with no eligibility sections and sets `noindex`; `SeoService` sets and replaces tags without duplicates; 404 renders the not-found content
- [ ] `npm test -- --watch=false`, `npm run lint` and `npm run build` (within budgets) pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
