---
id: T-030
title: Report-an-inaccuracy flag and dialog on cards and detail page
phase: 4
area: web
model: haiku
status: done
depends_on: [T-028, T-029, T-032]
requirements: [FR-08]
issue: 39
---

## Goal
A small flag icon on every result card and on the detail page opens an accessible dialog where anyone can report wrong information, without signing in.

## Context
- `AGENTS.md`, `specs/05-web.md` §5 (`ReportButtonComponent`, `features/report`), **§6 (Report rules)**, §7 (a11y)
- `tasks/T-023-report-inaccuracy-endpoint.md` (request, 201/400/404/429)
- `src/web/src/app/core/api/report.api.ts` (or equivalent from T-026)

## Scope
You may create or modify **only**:
- `src/web/src/app/features/report/**`
- `src/web/src/app/shared/ui/report-button/**`, `shared/ui/toast/**`
- `src/web/src/app/features/search/bursary-row/**`, `src/web/src/app/features/search/result-list/**` and `src/web/src/app/features/bursary/**` (to place the report button / wire the row's existing `report` output to the dialog). *(Updated 2026-11-02: T-034 replaced cards with `bursary-row`.)*

## Requirements
1. `ReportButtonComponent`: an icon-only button (flag SVG, `aria-label="Report a problem with this listing"`). Clicking it on a row must **not** follow the row's stretched link (stop propagation and prevent default).
2. `ReportDialogComponent` (CDK Dialog): title "Report a problem"; required reason radio group (labels: Wrong closing date, Wrong eligibility, Broken link, Bursary has closed, Duplicate listing, Something else); comment textarea (max 1000, live counter); optional email (validated); Submit and Cancel. Focus is trapped, Esc closes it, and focus returns to the flag button.
3. Submit → `ReportApi.create`. While sending, the button is disabled. `201` → close and show the toast "Thanks, we'll check it". `400` → inline field errors from `ApiError.errors`. `429` → inline "Too many reports from your connection. Please try again later." Other errors → inline generic message with retry.
4. A minimal `ToastService` + `ToastComponent` (polite `aria-live` region, auto-dismiss after 4 s).

## Acceptance criteria
- [ ] Tests: clicking the flag on a card opens the dialog and doesn't navigate; reason is required; comment max length; email validation; 201 closes and shows the toast; 400 shows field errors; 429 shows the rate-limit message; Esc closes and returns focus
- [ ] `npm test -- --watch=false`, `npm run lint` and `npm run build` (within budgets) pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
