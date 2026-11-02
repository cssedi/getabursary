---
id: T-034
title: Restyle search results as the deadline ledger, with mobile filter sheet
phase: 4
area: web
model: sonnet
status: todo
depends_on: [T-032]
requirements: [FR-03, FR-04, FR-05, FR-06]
issue: 57
---

## Goal
Search results become "ledger rows" (big date column + details, hairline separators, no cards or shadows), closing-soon dates get the highlighter swipe (the one animation), and on mobile the filters open in a bottom sheet.

## Context
- `AGENTS.md` (incl. "Frontend work"), **`specs/06-design.md` (governs; read all of it)**, `specs/05-web.md` (§5 Search wireframe, §6 principles 1, 2, 5, §7)
- Existing: `src/web/src/app/features/search/**` (BursaryCardComponent, ResultListComponent, FilterPanelComponent from T-027/T-028), `shared/ui/closing-date-badge` (reworked in T-032)

## Scope
You may create or modify **only**:
- `src/web/src/app/features/search/**`
- `src/web/src/app/shared/ui/source-icons/**`, `empty-state/**`, `error-state/**`, `skeleton-card/**`

## Requirements
1. Rename `BursaryCardComponent` to `BursaryRowComponent` (update usages and tests). Layout per the 06 §5 wireframe: 64 px date column (the T-032 date block) + details column (title link, funder, "Undergraduate, min 65% average", "Covers tuition and books", source icons with "found on N sites"). Separator: 1 px `--line` hairline. No radius, no shadow, no gradient.
2. The title `<a>` is the link, stretched over the row (`::after` inset 0) so the whole row is clickable; the report flag sits above the stretched link (`position: relative; z-index`). Modifier and middle-click work.
3. **The one animation:** on first render of a results page, the highlighter behind closing-soon dates animates `transform: scaleX(0→1)` from the left, 300 ms, stagger 60 ms, max 3 rows. Only `transform`. Under `prefers-reduced-motion: reduce` it renders already drawn. Nothing else on the page animates.
4. Closed rows: date shows "Closed" in `--closed`, the title isn't a link, `aria-disabled="true"`, still shows source icons.
5. Long content: titles wrap (max 3 lines then clamp), funder names truncate with an ellipsis, `min-width: 0` on flex children.
6. Count line "23 bursaries match" (`aria-live="polite"`) and the sort control on the same row, separate elements (no middle dot).
7. Mobile (< 1024 px): filters move into a CDK-dialog bottom sheet opened by a "Filters (3)" button; `overscroll-behavior: contain`; Esc and a "Show 23 bursaries" button close it; the sheet's open state is reflected in the URL (`filters=open`). ≥ 1024 px: left column as now.
8. Copy per 06 §8 for empty and error states.

## Acceptance criteria
- [ ] Tests: row renders the date block, title link and details; stretched-link click target; the flag doesn't navigate; closed row has no link; reduced motion → no animation class; long title clamps; the filter sheet opens/closes, syncs `filters=open` and reports the count
- [ ] `grep -rn "shadow\|gradient\|rounded-xl\|rounded-2xl" src/web/src/app/features/search` returns nothing
- [ ] PR has the "Design checklist" section
- [ ] `npm test -- --watch=false`, `npm run lint` and `npm run build` pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
