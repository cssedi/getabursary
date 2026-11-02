---
id: T-023
title: Report-an-inaccuracy endpoint with rate limiting
phase: 3
area: api
model: haiku
status: todo
depends_on: [T-003, T-007]
requirements: [FR-08, NFR-04]
issue: 23
---

## Goal
Anyone (no login) can report an inaccurate listing. The report lands in the admin review queue, and abuse is limited.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §1–§3, §7 (ip hashing, rate limits)
- `specs/00-product-brief.md` FR-08
- `specs/01-architecture.md` §6, §7
- `specs/02-data-model.md` §1 (`ReportReason`), §2.4

## Scope
You may create or modify **only**:
- `src/GetABursary.Api/Controllers/ReportsController.cs`
- `src/GetABursary.Api/Services/Reports/**`, `src/GetABursary.Api/DTOs/Reports/**`
- `src/GetABursary.Api/Extensions/ReportsServiceCollectionExtensions.cs`, `src/GetABursary.Api/Program.cs` (register the feature + rate limiter + `UseRateLimiter()`)
- `src/GetABursary.Api/appsettings.json` (non-secret `Reports:*` defaults; salt stays empty)
- `src/GetABursary.Core/Repositories/Reports/IListingReportRepository.cs`, `ListingReportRepository.cs`
- `tests/GetABursary.Api.Tests/Reports/**`
- `tests/GetABursary.Api.Tests/**` (widened by orchestrator 2026-11-02: Postgres-backed API test infrastructure)

## Requirements
1. `POST /api/v1/bursaries/{id:guid}/reports` with body `CreateReportRequest { reason (ReportReason), comment? (≤ 1000), contactEmail? (valid email, ≤ 254) }` → `201 Created` with `{ id }`.
2. Unknown bursary → 404. Invalid body → 400 with field errors.
3. Creates a `ListingReport` **and** a `ReviewItem(UserReport)` linked to it, in one `SaveChangesAsync`.
4. `ip_hash` = hex SHA-256 of `Reports:IpHashSalt + remoteIp`. Startup fails fast in non-Development environments if the salt is empty.
5. Rate limit policy `reports`: fixed window, **5 requests per hour per IP** → `429` with ProblemDetails.

## Acceptance criteria
- [ ] Integration tests: valid → 201 + report + review item rows; invalid reason/email/too-long comment → 400; unknown bursary → 404; 6th request in a window → 429
- [ ] Unit test: the raw IP is never stored (only the hash)
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Reports"
```
