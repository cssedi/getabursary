---
id: T-046
title: Admin tab - sources page with run now and run history (web)
phase: 5
area: web
model: haiku
status: todo
depends_on: [T-044, T-045]
requirements: [FR-43]
issue: 83
---

## Goal
Admins see every scraper source's health at a glance, switch sources on or off, and press "Run now".

## Context
- `AGENTS.md` (incl. "smallest complete change"), `specs/03-conventions.md`, **`specs/07-accounts-and-admin.md`**, `specs/adr/ADR-003-cookie-auth.md` (§2 Sources)
- Frontend: **`specs/06-design.md` governs** (tokens, type, §7 checklist, §8 copy); `specs/05-web.md`

## Scope
- `src/web/src/app/features/admin/sources/**`, `src/web/src/app/core/api/**` (source calls), the admin routes file (one route)

## Requirements
1. Table: source name, enabled toggle (a real `<input type="checkbox" role="switch">` with a label), last run (relative time via `Intl.RelativeTimeFormat('en-ZA')` + absolute in `title`), counts (discovered, created, updated, failed), last error (truncated, expandable).
2. "Run all sources now" button and a per-row "Run now". After clicking: "Run requested. The worker starts it within a minute." Poll the list every 15 s for 2 minutes, then stop.
3. Recent runs list below (paged).

## Acceptance criteria
- [ ] Tests: renders sources, toggles enabled with rollback on error, run-now calls the endpoint and shows the message, polling stops after 2 minutes (fake timers)
- [ ] "Design checklist" in the PR; `npm test -- --watch=false`, `npm run lint`, `npm run build` pass; Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
