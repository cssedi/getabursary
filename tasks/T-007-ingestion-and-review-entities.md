---
id: T-007
title: Add ingestion, report and review-queue entities
phase: 1
area: core
model: haiku
status: todo
depends_on: [T-006]
requirements: [FR-08, FR-22, FR-25, FR-27, FR-41]
issue: 7
---

## Goal
Tables exist for raw listing snapshots, ingestion run tracking, manual run requests, user inaccuracy reports and the admin review queue.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §3, §4
- `specs/02-data-model.md` §1 (enums `ReportReason`, `ReviewItemType`, `ReviewStatus`, `IngestionTrigger`, `RunStatus`), §2.3 and §2.4

## Scope
You may create or modify **only**:
- `src/GetABursary.Core/Domain/Ingestion/**`, `src/GetABursary.Core/Domain/Reports/**`, `src/GetABursary.Core/Domain/Common/Enums/**`
- `src/GetABursary.Core/Data/Configurations/Ingestion/**`, `src/GetABursary.Core/Data/Configurations/Reports/**`
- `src/GetABursary.Core/Data/GetABursaryDbContext.cs` (add `DbSet`s only)
- `src/GetABursary.Core/Data/Migrations/**` (one new migration `AddIngestionAndReview`)
- `tests/GetABursary.Core.Tests/Data/Ingestion/**`, `tests/GetABursary.Core.Tests/Data/Reports/**`

## Requirements
1. Entities: `RawListing`, `IngestionRun`, `IngestionRunSource`, `IngestionRunRequest`, `ListingReport`, `ReviewItem` (columns as in spec §2.3–2.4). `RawListing.Parsed`, `RawListing.Extracted` and `ReviewItem.Details` are `jsonb` columns stored as `JsonDocument?`.
2. Unique index on `raw_listing` (`source_id`, `url`). Index on `review_item` (`status`, `type`).
3. `ListingReport.Comment` max length 1000. `ReviewItem.Notes` max length 2000.
4. Migration `AddIngestionAndReview`.

## Acceptance criteria
- [ ] Integration tests: save and reload each entity, including the jsonb columns
- [ ] Integration test: a duplicate (`source_id`, `url`) raw listing throws `DbUpdateException`
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Data.Ingestion|FullyQualifiedName~Data.Reports"
```
