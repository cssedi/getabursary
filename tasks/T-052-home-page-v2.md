---
id: T-052
title: Real home page - hero, working degree search, closing soon, how it works, browse, sources
phase: 7
area: web
model: sonnet
status: done
depends_on: [T-051]
requirements: [FR-01, FR-02, FR-05]
issue: 102
---

## Goal
A welcoming home page that explains the platform in seconds and lets a student pick their university and qualification without anything breaking.

## Context
- `AGENTS.md`, **`specs/06-design.md` v2 (governs; read all of it, including §7 checklist and §9 visual check)**, `specs/05-web.md`
- Skills: frontend-design and web-design-guidelines principles are already distilled into spec 06; follow the spec (§5 Home in full, §6)
- `specs/01-architecture.md` §6 (taxonomy endpoints), existing `features/home/**`

## Scope
- `src/web/src/app/features/home/**` (rebuild), `src/web/src/app/core/api/**` (new call)
- API: `src/GetABursary.Api/Controllers/TaxonomyController.cs`, `src/GetABursary.Api/Services/Taxonomy/**`, `src/GetABursary.Api/DTOs/Taxonomy/**`, `src/GetABursary.Core/Repositories/Taxonomy/**`, `tests/GetABursary.Api.Tests/Taxonomy/**`
- `docs/screenshots/T-052/**`

## Requirements
1. API: `GET /api/v1/taxonomy/institutions/{id}/qualifications` → `[{ facultyId, facultyName, qualifications: [{ id, name, level }] }]`, faculties and qualifications sorted by name, one query. Unknown id → 404.
2. Search panel per §5: University → Qualification (one `<select>` with `<optgroup>` per faculty, from the new endpoint) → Year of study → Average → "Show bursaries" (navigates to `/search?...` with the chosen values; works with any subset). Before a university is chosen the qualification select is disabled and says "Pick a university first". Loading shows "Loading qualifications…". Changing university clears qualification. Works before and after hydration (event replay from T-051).
3. Hero per §4/§5 with the hand-drawn calendar illustration (inline SVG, `aria-hidden`, the swipe draws once; reduced motion → no animation).
4. "Closing soon": 5 rows from `sort=ClosingSoonest&pageSize=5` (server-rendered), each linking to the bursary, + "See all closing soon" link. Empty → "No bursaries close in the next while. Search by your degree instead."
5. "How it works" (3 numbered steps), "Browse by field" (all top-level fields as large links to `/bursaries/field/:slug`), "Where the listings come from" (enabled sources by name, linking to their sites).
6. Below-the-fold sections use `@defer (on viewport; hydrate on viewport)` with reserved height (no layout shift).

## Acceptance criteria
- [ ] API tests: grouped list, sort order, 404
- [ ] Web tests: qualification select disabled → enabled after university, groups rendered, submit builds the right URL, closing-soon empty state
- [ ] Screenshots of `/` (mobile + desktop) committed and reviewed by you; the select behaviour is covered by the web tests
- [ ] "Design checklist" in the PR; `dotnet test`, `npm test -- --watch=false`, `npm run lint`, `npm run build` pass; Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Taxonomy"
cd src/web && npm ci && npm test -- --watch=false && npm run build && cd ../..
node scripts/visual-check.mjs T-052 /
```
