---
id: T-053
title: Search results v2 - no-overlap layout, filter sheet, grouped fields, ledger rows, named "Found on" source links
phase: 7
area: web
model: sonnet
status: done
depends_on: [T-051]
requirements: [FR-01, FR-02, FR-03, FR-04, FR-05, FR-06]
issue: 103
---

## Goal
The results page is steady and readable at every width, and each bursary lists every site it was found on, by name, each linking to that site's page for the bursary.

## Context
- `AGENTS.md`, **`specs/06-design.md` v2 (governs; read all of it, including §7 checklist and §9 visual check)**, `specs/05-web.md`
- Skills: frontend-design and web-design-guidelines principles are already distilled into spec 06; follow the spec (§5 Search and the "Found on" rule, §6)
- Current `features/search/**`, `shared/ui/source-icons/**`; the API already returns `sources[{ key, name, iconUrl, url }]` where `url` is the exact aggregator page

## Scope
- `src/web/src/app/features/search/**`, `src/web/src/app/shared/ui/source-links/**` (new; replaces `source-icons`, delete it and update its users), `docs/screenshots/T-053/**`
- `tests/GetABursary.Api.Tests/Bursaries/Search/**` (one test only: a bursary on 3 sources returns 3 sources with their listing URLs)

## Requirements
1. Layout per §5: 288 px sticky filter column ≥ 1024 px (own scroll, `min-width: 0` on the results column so nothing overlaps); < 1024 px "Filters (n)" opens a bottom sheet; applying filters updates the URL.
2. Field of study: `<details>` groups with a filter-as-you-type box; open only groups with selections; "Clear" per group.
3. Ledger rows per §5: date block (highlighter for ≤ 14 days, "Closed" treatment), title link, funder, one line of level + min average, **Found on** line via `SourceLinksComponent`: favicon + name for every source, each an `<a href=url target=_blank rel="noopener nofollow">`, separated by commas; long names wrap. Save and report buttons on the right, ≥ 44 px targets.
4. Smoothness: filter changes debounce 300 ms; keep the previous results visible (dimmed, `aria-busy`) while loading instead of swapping to skeletons; skeletons only on first load; `track` by id; no animation except §6.5.
5. Funder text: show at most 2 lines (`line-clamp`), full text in `title`.

## Acceptance criteria
- [ ] Tests: SourceLinks renders every source as a named link with the right href/rel/target; filters sheet opens below 1024 px; debounce; previous results stay during loading
- [ ] API test as in Scope
- [ ] Screenshots: `/search` with no filters, with a field selected, and a long-name case, mobile + desktop, committed and reviewed by you; nothing overlaps
- [ ] "Design checklist" in the PR; `npm test -- --watch=false`, `npm run lint`, `npm run build`, `dotnet test` pass; Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run build && cd ../..
node scripts/visual-check.mjs T-053 /search "/search?studyLevel=Undergraduate"
```
