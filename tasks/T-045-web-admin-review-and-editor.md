---
id: T-045
title: Admin tab - review queue and bursary editor (web)
phase: 5
area: web
model: sonnet
status: todo
depends_on: [T-040, T-043]
requirements: [FR-41, FR-42]
issue: 82
---

## Goal
An `/admin` area where admins clear the review queue (publish, dismiss, mark closed) and edit any bursary, seeing which fields are locked.

## Context
- `AGENTS.md` (incl. "smallest complete change"), `specs/03-conventions.md`, **`specs/07-accounts-and-admin.md`**, `specs/adr/ADR-003-cookie-auth.md` (§2)
- Frontend: **`specs/06-design.md` governs** (tokens, type, §7 checklist, §8 copy); `specs/05-web.md`

## Scope
- `src/web/src/app/features/admin/**` (new), `src/web/src/app/core/api/**` (admin calls), `app.routes.ts` (admin routes + guard)

## Requirements
1. Route guard: only `isAdmin()` users; others see the 404 page (don't reveal the admin area). Admin pages set `<meta name="robots" content="noindex">`.
2. `/admin` layout: a simple sub-nav "Review queue", "Bursaries", "Sources" (Sources page comes in T-046; link it anyway).
3. Review queue: type filter, list (bursary name, funder, closing date block, reason, report comment), actions Publish / Edit / Dismiss (with an optional note) / Mark closed. Optimistic removal from the list with undo-free error rollback. Count of open items.
4. Bursaries: search box (debounced) + published filter; rows link to the editor.
5. Editor `/admin/bursaries/:id`: a form for the 07 §2 fields (dates via `<input type="date">`, multi-selects for levels, fields and coverage), "Locked" badge + "Unlock" per locked field, "Save changes" (PATCH only changed fields), "Publish"/"Unpublish". Inline validation per 06 §7. Warn before leaving with unsaved changes.
6. Dense but calm: the same tokens and type as the public site; tables are fine here.

## Acceptance criteria
- [ ] Tests: the guard blocks non-admins; queue actions call the right endpoints and update the list; the editor sends only changed fields, shows locked badges and unlocks; the unsaved-changes guard
- [ ] "Design checklist" in the PR; `npm test -- --watch=false`, `npm run lint`, `npm run build` pass; Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
