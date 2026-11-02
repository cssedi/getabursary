---
id: T-036
title: Web interface guidelines audit and fixes across the web app
phase: 4
area: web
model: sonnet
status: todo
depends_on: [T-030, T-031, T-033, T-034, T-035]
requirements: [NFR-01, NFR-02]
issue: 59
---

## Goal
Every file under `src/web/src/` passes the 06 §7 interface checklist, with an audit report committed so the result is visible.

## Context
- `AGENTS.md` (incl. "Frontend work"), **`specs/06-design.md` (governs; read all of it)**, `specs/05-web.md` (**§7 in full**)

## Scope
You may create or modify **only**:
- `src/web/src/**` (fixes only; no new features)
- `docs/design-audit.md` (new)

## Requirements
1. Review every `.ts`, `.html` and `.css` file under `src/web/src/app/` plus `index.html` and `styles.css` against **every** 06 §7 rule.
2. Write `docs/design-audit.md` in this format: one heading per file; each finding as `path:line` + a terse issue + "→ fix"; files with no issues listed as "✓ pass". Then a "Fixed in this PR" list and a "Not fixed (with reason)" list.
3. Fix every finding you can within scope. Anti-patterns (06 §7 last line) must all be fixed.
4. Also check 06 §4/§8: sentence case, no all-caps labels, no middle-dot meta, no "→" in button text, no "we/I" in UI copy, curly quotes and "…".

## Acceptance criteria
- [ ] `docs/design-audit.md` exists and covers every file
- [ ] Zero anti-patterns remain (list the grep commands you used in the PR)
- [ ] Existing tests still pass; add tests for any behaviour you changed (e.g. focus moving to the first error)
- [ ] `npm test -- --watch=false`, `npm run lint` and `npm run build` pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
grep -rnE "transition:\s*all|outline:\s*none|user-scalable=no|maximum-scale=1|\(click\)=\"[^\"]*navigate" src || echo clean
```
