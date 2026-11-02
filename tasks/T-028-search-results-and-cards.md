---
id: T-028
title: Search results list, bursary cards, source icons, closed state and paging
phase: 4
area: web
model: sonnet
status: done
depends_on: [T-027]
requirements: [FR-03, FR-04, FR-05, FR-06, NFR-01]
issue: 37
---

## Goal
`/search` shows matching bursaries as cards with closing-date badges and source icons. Closed bursaries are greyed out and not clickable, there's paging, and every loading, empty and error state is handled.

## Context
- `AGENTS.md`, **`specs/05-web.md` §5 (search, shared/ui), §6 (all rules), §7**
- `specs/00-product-brief.md` FR-03–FR-06
- `src/web/src/app/core/api/**`, `src/web/src/testing/fixtures.ts`, `features/search/search-state.service.ts`

## Scope
You may create or modify **only**:
- `src/web/src/app/features/search/result-list/**`, `bursary-card/**`, `pagination/**`, and `search.page.*` (replace the placeholder)
- `src/web/src/app/shared/ui/source-icons/**`, `closing-date-badge/**`, `empty-state/**`, `error-state/**`, `skeleton-card/**`

## Requirements
1. `SearchPage` calls `BursaryApi.search(state.apiParams())` whenever the criteria change (cancel the in-flight request on change). During SSR the first page renders on the server, with no duplicate request in the browser (use the HTTP transfer cache).
2. `BursaryCardComponent` exactly per spec §6 (card fields, closing-soon badge ≤ 14 days, coverage "+N", whole card links to `/bursaries/:slug`; closed: greyed, "Closed" badge, no link, `aria-disabled`).
3. `SourceIconsComponent`: per spec §6, including the initials fallback when an icon fails to load.
4. `ClosingDateBadgeComponent`: "Closes 31 Mar 2027" plus "Closes in N days" (accent colour) when ≤ 14 days, "Closes today", "Closed". Uses an injectable clock/`Date` provider so tests are deterministic.
5. `ResultListComponent`: total count ("23 bursaries match"), cards, a hidden `.ad-slot` placeholder after every 6th card (spec §7), skeletons while loading, the empty state with a "Clear filters" action, and the error state with retry.
6. `PaginationComponent`: previous/next plus "Page X of Y"; updates `page` in the URL; scrolls to the top of the results.

## Acceptance criteria
- [ ] Tests using fixtures: an open card links to `/bursaries/{slug}`; a closed card has no link, has `aria-disabled`, and shows "Closed"; the badge thresholds at 0, 1, 14 and 15 days; source icons render per source and fall back to initials on error; empty, error (retry calls the API again) and loading states; the ad-slot placeholder appears after 6 cards and is hidden; paging updates the URL
- [ ] `npm test -- --watch=false`, `npm run lint` and `npm run build` (within budgets) pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
