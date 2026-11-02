---
id: T-044
title: Admin API - sources, run history and "run now"
phase: 5
area: api
model: haiku
status: done
depends_on: [T-038]
requirements: [FR-43]
issue: 81
---

## Goal
Admins see each scraper source's health and last run, switch sources on or off, and trigger a run without touching a server.

## Context
- `AGENTS.md` (incl. "smallest complete change"), `specs/03-conventions.md`, **`specs/07-accounts-and-admin.md`**, `specs/adr/ADR-003-cookie-auth.md` (§2 Sources)
- `specs/02-data-model.md` §2.3; the worker polls `ingestion_run_request` (T-011)

## Scope
- `src/GetABursary.Api/Controllers/Admin/SourcesController.cs`, `src/GetABursary.Api/Services/Admin/**`, `src/GetABursary.Api/DTOs/Admin/**`, `src/GetABursary.Core/Repositories/Ingestion/**` (read methods, plus the two writes this task needs: set `Source.Enabled`, insert `IngestionRunRequest`. Orchestrator widening 2026-11-02)
- `tests/GetABursary.Api.Tests/Admin/**`

## Requirements
1. `[Authorize(Roles = "Admin")]` everywhere.
2. `GET /api/v1/admin/sources` → `[{ id, key, name, enabled, lastRun: { startedAt, finishedAt, status, discovered, created, updated, failed, errorSummary } | null }]` (latest `ingestion_run_source` per source in one query).
3. `PATCH /api/v1/admin/sources/{id}` `{ enabled }`.
4. `POST /api/v1/admin/ingestion-runs` `{ sourceId? }` → inserts an `ingestion_run_request` (requested_by = current admin) → 202. If an unpicked request already exists, return 202 with it (no duplicates).
5. `GET /api/v1/admin/ingestion-runs?page=` → recent runs with per-source counts.

## Acceptance criteria
- [ ] Integration tests: auth 401/403; list with and without a last run; toggle; run-now inserts one request and a second call doesn't duplicate; run history paging
- [ ] `dotnet build` and `dotnet test` pass; Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Admin"
```

## Orchestrator decisions (2026-11-02, answering the BLOCKED comment)
- Option **A**: add the three registrations to `AdminServiceCollectionExtensions.cs` (now also covered by the standing DI allowance in AGENTS.md).
- Check-then-insert for "run now" is fine. The rare double request is harmless (the worker runs once per picked request). No migration.
- An unknown `sourceId` returns 404, a disabled source is accepted, and PATCH returns the list-item shape: all agreed.
