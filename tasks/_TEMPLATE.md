---
id: T-0xx
title: Short imperative title
phase: 0
area: core            # infra | core | api | worker | web | data | docs
model: haiku          # haiku (mechanical) | sonnet (design-heavy)
status: todo          # todo | ready | in-progress | done | blocked
depends_on: []
requirements: []      # e.g. [FR-01, NFR-06]
issue: null           # filled in by scripts/sync-issues.mjs
---

## Goal
One or two sentences: what exists after this task that didn't before.

## Context
Read these before starting:
- `AGENTS.md`, `specs/03-conventions.md`
- `specs/…#section`

## Scope
You may create or modify **only**:
- `path/…`

Out of scope: …

## Requirements
Precise, numbered implementation details (names, signatures, behaviour).

## Acceptance criteria
- [ ] …
- [ ] `dotnet build` passes with zero warnings; `dotnet test` passes
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~…"
```
