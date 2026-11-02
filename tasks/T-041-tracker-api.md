---
id: T-041
title: Saved bursaries and application tracker API
phase: 5
area: api
model: haiku
status: done
depends_on: [T-038]
requirements: [FR-12, FR-13, FR-14]
issue: 78
---

## Goal
Signed-in students can save bursaries and track each application's status and notes.

## Context
- `AGENTS.md` (incl. "smallest complete change"), `specs/03-conventions.md`, **`specs/07-accounts-and-admin.md`**, `specs/adr/ADR-003-cookie-auth.md` (§1 "Saved bursaries & tracker")
- `specs/02-data-model.md` §1 (`TrackerStatus`), §2.5

## Scope
- `src/GetABursary.Core/Domain/Users/TrackerEntry.cs`, `src/GetABursary.Core/Data/**` (config + **one** migration `AddTrackerEntries`), `src/GetABursary.Core/Repositories/Tracker/**`
- `src/GetABursary.Api/Controllers/TrackerController.cs`, `src/GetABursary.Api/Services/Tracker/**`, `src/GetABursary.Api/DTOs/Tracker/**`, `src/GetABursary.Api/Extensions/TrackerServiceCollectionExtensions.cs`, `src/GetABursary.Api/Program.cs` (one line)
- `tests/GetABursary.Api.Tests/Tracker/**`

## Requirements
1. `GET /api/v1/me/tracker` → `[{ bursary: BursarySummaryResponse, status, appliedOn?, notes?, updatedAt }]`, closing soonest first. One query (no per-item lookups).
2. `PUT /api/v1/me/tracker/{bursaryId}` body `{ status, appliedOn?, notes? }` → upsert (201 on create, 200 on update). Unknown or unpublished bursary → 404. `notes` ≤ 2000.
3. `DELETE /api/v1/me/tracker/{bursaryId}` → 204 (idempotent).
4. All require auth (401 otherwise); a user only ever sees their own entries (filter by the cookie's user id, never by a request parameter).

## Acceptance criteria
- [ ] Integration tests: upsert create/update; list order; delete; 401 signed out; user B can't see or delete user A's entries; unknown bursary 404; notes too long 400
- [ ] `dotnet build` and `dotnet test` pass; Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Tracker"
```
