---
id: T-043
title: Admin API - review queue, publish/close, bursary search and edit with locked fields
phase: 5
area: api
model: sonnet
status: done
depends_on: [T-038]
requirements: [FR-40, FR-41, FR-42]
issue: 80
---

## Goal
Admins can work through the review queue (publish, dismiss, mark closed), find any bursary and edit it. Edited fields stay locked against re-ingestion.

## Context
- `AGENTS.md` (incl. "smallest complete change"), `specs/03-conventions.md`, **`specs/07-accounts-and-admin.md`**, `specs/adr/ADR-003-cookie-auth.md` (§2)
- `specs/02-data-model.md` §2.2 (`locked_fields`), §2.4 (`review_item`, `listing_report`); the existing merger respects `LockedFields` (T-018)

## Scope
- `src/GetABursary.Api/Controllers/Admin/**`, `src/GetABursary.Api/Services/Admin/**`, `src/GetABursary.Api/DTOs/Admin/**`, `src/GetABursary.Api/Extensions/AdminServiceCollectionExtensions.cs`, `src/GetABursary.Api/Program.cs` (one line)
- `src/GetABursary.Core/Repositories/Admin/**` (or extend the existing bursary/review repositories)
- `tests/GetABursary.Api.Tests/Admin/**`

## Requirements
1. Every endpoint: `[Authorize(Roles = "Admin")]` (students → 403, signed out → 401).
2. `GET /api/v1/admin/review-items?type=&page=` (open items, newest first, with bursary summary + reason + report comment if any).
3. `POST /api/v1/admin/review-items/{id}/resolve` body `{ action: Publish | Dismiss | MarkClosed, notes? }`: Publish sets `is_published=true`; MarkClosed sets status Closed and locks `Status`; all resolve the item (who, when, notes). One transaction.
4. `GET /api/v1/admin/bursaries?query=&published=&page=` (trigram name search, includes unpublished).
5. `GET /api/v1/admin/bursaries/{id}` and `PATCH /api/v1/admin/bursaries/{id}` with any subset of editable fields (07 §2). Each changed field name is added to `locked_fields`. `DELETE /api/v1/admin/bursaries/{id}/locks/{field}` unlocks one field.

## Acceptance criteria
- [ ] Integration tests: 401/403 for non-admins; list + filter; publish makes the bursary appear in public search; dismiss; mark closed; PATCH updates and locks only the changed fields; a merge after PATCH doesn't overwrite a locked field (use the existing merger); unlock
- [ ] `dotnet build` and `dotnet test` pass; Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Admin"
```
