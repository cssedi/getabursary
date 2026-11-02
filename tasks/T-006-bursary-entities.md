---
id: T-006
title: Add bursary, funder and source entities
phase: 1
area: core
model: sonnet
status: done
depends_on: [T-005]
requirements: [FR-04, FR-05, FR-06, FR-07, FR-24, FR-42]
issue: 6
---

## Goal
Canonical bursaries are stored with structured eligibility, links to fields, qualifications and institutions, and the aggregator sources each one was found on.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §3, §4
- `specs/02-data-model.md` §1 (enums `BursaryStatus`, `Citizenship`, `CoverageItem`) and §2.2 (bursary tables)

## Scope
You may create or modify **only**:
- `src/GetABursary.Core/Domain/Bursaries/**`, `src/GetABursary.Core/Domain/Common/Enums/**` (add the new enums)
- `src/GetABursary.Core/Data/Configurations/Bursaries/**`
- `src/GetABursary.Core/Data/GetABursaryDbContext.cs` (add `DbSet`s only)
- `src/GetABursary.Core/Data/Migrations/**` (one new migration `AddBursaries`)
- `tests/GetABursary.Core.Tests/Data/Bursaries/**`

## Requirements
1. Entities: `Funder`, `Bursary`, `Source`, and join entities `BursaryField`, `BursaryQualification`, `BursaryInstitution`, `BursarySource` (columns as in spec §2.2).
2. Postgres arrays: `Coverage` (`List<CoverageItem>`), `StudyLevels` (`List<StudyLevel>`), `Provinces` (`List<Province>`) and `LockedFields` (`List<string>`) map to `text[]` (enum arrays converted to string arrays).
3. `MinSubjectRequirements` maps to `jsonb` as an owned collection or JSON column of `SubjectRequirement(string Subject, decimal MinPercent)`.
4. Indexes: unique `slug`. GIN trigram index on `normalized_name` (`HasMethod("gin").HasOperators("gin_trgm_ops")`). Composite index on (`status`, `closing_date`). Unique `funder.normalized_name`. Unique `source.key`.
5. Add a domain method `Bursary.IsClosingSoon(DateOnly today)`: true if `Status == Open`, `ClosingDate` is not null, and today ≤ ClosingDate ≤ today + 14 days.
6. Add a domain method `Bursary.IsLocked(string fieldName)`.
7. Migration `AddBursaries`.

## Acceptance criteria
- [ ] Integration test: a bursary with coverage, study levels, provinces, subject requirements, 2 fields, 1 institution restriction and 2 sources saves and reloads with every value intact
- [ ] Integration test: the trigram index exists and `similarity(normalized_name, 'x')` can be queried via `EF.Functions.TrigramsSimilarity`
- [ ] Unit tests for `IsClosingSoon` (open + 14 days, open + 15 days, closed, null date) and `IsLocked`
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Bursaries"
```
