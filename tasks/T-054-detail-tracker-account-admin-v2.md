---
id: T-054
title: Detail, tracker, sign-in and admin pages in the v2 design, with "Found on" on the detail page
phase: 7
area: web
model: sonnet
status: done
depends_on: [T-051, T-053]
requirements: [FR-06, FR-07, FR-12, FR-13, FR-41]
issue: 104
---

## Goal
Every remaining page matches v2, and the bursary detail page shows where it was found with links to each source page.

## Context
- `AGENTS.md`, **`specs/06-design.md` v2 (governs; read all of it, including §7 checklist and §9 visual check)**, `specs/05-web.md`
- Skills: frontend-design and web-design-guidelines principles are already distilled into spec 06; follow the spec (§5 Detail and "Everything else")

## Scope
- `src/web/src/app/features/bursary/**`, `features/tracker/**`, `features/account/**`, `features/admin/**`, `features/static/**` (restyle and layout only; no behaviour changes), `docs/screenshots/T-054/**`

## Requirements
1. Detail: 680 px column, date block, title, funder, "Found on" (`SourceLinksComponent` from T-053), sections in Newsreader reading text, sticky "Apply on {funder}'s site" bar on mobile (safe-area insets), closed bursaries as 05 §6.
2. Tracker and admin: v2 tokens, hairline tables/rows, no overlaps at 320 px (tables scroll horizontally inside their own container on mobile, never the page).
3. Sign-in dialog and account pages: v2 controls; dialog is a bottom sheet on mobile.

## Acceptance criteria
- [ ] Screenshots: a detail page (open, closing soon, closed), `/my-bursaries` and `/admin` (use `--sign-in-admin`), mobile + desktop, committed and reviewed by you
- [ ] Existing tests still pass (update markup assertions under the standing allowance)
- [ ] "Design checklist" in the PR; `npm test -- --watch=false`, `npm run lint`, `npm run build` pass; Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run build && cd ../..
node scripts/visual-check.mjs T-054 --sign-in-admin /bursaries/sasol-bursary /my-bursaries /admin
```
