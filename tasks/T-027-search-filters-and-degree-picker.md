---
id: T-027
title: Degree picker, filter panel and URL-driven search state
phase: 4
area: web
model: sonnet
status: todo
depends_on: [T-026]
requirements: [FR-01, FR-02, FR-09, NFR-01]
issue: 36
---

## Goal
Students pick University → Faculty → Qualification (or a field of study) and set study level, average, closing dates and "show closed". All of it lives in the URL so searches are shareable, and the home page's picker starts a search.

## Context
- `AGENTS.md`, `specs/05-web.md` §3 (`/`, `/search`), **§4 (URL schema)**, §5 (home, search), §6 (states), §7 (a11y)
- `specs/00-product-brief.md` FR-01, FR-09 (filters are URL-only), FR-32 (picker shows institutions with qualifications only)
- `src/web/src/app/core/api/**` (from T-026)

## Scope
You may create or modify **only**:
- `src/web/src/app/features/home/**`
- `src/web/src/app/features/search/search-state.service.ts`, `filter-panel/**`, `search.page.ts` (+ its template/styles/spec), and the `search` route entry
- `src/web/src/app/shared/ui/**` (new shared components only)

## Requirements
1. `DegreePickerComponent`: three dependent `<select>`s (institution → faculty → qualification) loading from `TaxonomyApi`; a change resets the levels below; labels for each select; emits `{ institutionId, facultyId, qualificationId }`; can be pre-filled from inputs. While loading, the select shows "Loading…" and is disabled.
2. `HomePage`: headline "Find bursaries for your degree", the picker, a study-level select, an optional average input (0–100), and a "Search" button → `router.navigate(['/search'], { queryParams })` (push).
3. `SearchStateService`: reads/validates query params (spec §4) into a `criteria` signal; `update(partial)` writes the URL with `replaceUrl: true` and resets `page` to 1 (except when only `page` changes); exposes `apiParams` (`BursarySearchParams`, without `facultyId`).
4. `FilterPanelComponent` (on `/search`): the degree picker; a field-of-study multi-select (checkbox list grouped by first-order field, collapsible); study level; average (debounced 400 ms); closing from/to dates; "Show closed bursaries" checkbox (off by default); sort select; a "Clear filters" button. On mobile (< 768 px) the panel is a collapsible section above the results with an "N filters active" summary.
5. `SearchPage` shows the filter panel and, for now, a `<pre>` of `apiParams` as a placeholder for the result list (T-028 replaces it).

## Acceptance criteria
- [ ] Tests: the picker cascades and resets lower levels; home submit navigates with the right query params; `SearchStateService` round-trips URL ⇄ criteria, drops invalid values (`average=150`, unknown `sort`), resets `page` on filter change, and uses `replaceUrl`; "Show closed" defaults to off; clearing filters empties the URL
- [ ] Keyboard-only use works for every control (state how you checked in the PR)
- [ ] `npm test -- --watch=false`, `npm run lint` and `npm run build` (within budgets) pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
