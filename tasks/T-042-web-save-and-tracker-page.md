---
id: T-042
title: Save button on rows and detail, and the "My bursaries" tracker page
phase: 5
area: web
model: sonnet
status: todo
depends_on: [T-040, T-041]
requirements: [FR-09, FR-12, FR-13, FR-14]
issue: 79
---

## Goal
A save control on every open bursary (row and detail page) and a "My bursaries" page where students change each application's status, add notes and see what's closing soon.

## Context
- `AGENTS.md` (incl. "smallest complete change"), `specs/03-conventions.md`, **`specs/07-accounts-and-admin.md`**, `specs/adr/ADR-003-cookie-auth.md` (§1)
- Frontend: **`specs/06-design.md` governs** (tokens, type, §7 checklist, §8 copy); `specs/05-web.md`

## Scope
- `src/web/src/app/features/tracker/**` (new), `src/web/src/app/core/api/**` (tracker calls)
- `src/web/src/app/shared/ui/save-button/**` (new), `features/search/bursary-row/**` and `features/bursary/**` (place the button only), `app.routes.ts` (tracker route)

## Requirements
1. `SaveButtonComponent`: bookmark icon + visually-hidden label "Save {name}" / "Saved"; uses `requireAuth` (T-040), so signed-out users get the dialog and the save completes after sign-in. Optimistic toggle with rollback on error. Not shown on closed bursaries.
2. `/my-bursaries` (signed-in only; redirect to sign-in otherwise): the deadline-ledger rows (reuse the T-034 date block) with a status `<select>` per row (Saved, Preparing, Applied, Shortlisted, Awarded, Unsuccessful, Withdrawn), "Applied on" date, notes (debounced save, max 2000), remove. Counts per status at the top as plain text ("3 saved, 2 applied, 1 awarded"). Empty state: "Save bursaries to track them here." + link to search.
3. Copy and visuals per 06 (sentence case, the highlighter only for closing-soon).

## Acceptance criteria
- [ ] Tests: signed-out save opens the dialog and saves after sign-in; save/unsave toggles with rollback on error; the tracker page lists, updates status and notes, removes, and shows the empty state; closed bursaries have no save button
- [ ] "Design checklist" in the PR; `npm test -- --watch=false`, `npm run lint`, `npm run build` pass; Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
