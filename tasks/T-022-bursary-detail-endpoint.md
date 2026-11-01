---
id: T-022
title: Bursary detail endpoint (closed bursaries return 410 with summary)
phase: 3
area: api
model: haiku
status: todo
depends_on: [T-021]
requirements: [FR-06, FR-07]
issue: null
---

## Goal
`GET /api/v1/bursaries/{slug}` returns the full structured detail for open bursaries. Closed bursaries return `410 Gone` with only the summary, so the UI can grey them out without exposing details.

## Context
- `AGENTS.md`, `specs/03-conventions.md`
- `specs/00-product-brief.md` FR-06, FR-07
- `specs/01-architecture.md` §6 (detail row)
- `specs/02-data-model.md` §2.2, §5 (`BursarySummaryResponse`)

## Scope
You may create or modify **only**:
- `src/GetABursary.Api/Controllers/BursariesController.cs` (add one action)
- `src/GetABursary.Api/Services/Bursaries/IBursaryService.cs`, `BursaryService.cs`
- `src/GetABursary.Api/DTOs/Bursaries/BursaryDetailResponse.cs`
- `src/GetABursary.Api/Extensions/BursaryServiceCollectionExtensions.cs`
- `src/GetABursary.Core/Repositories/Bursaries/IBursaryReadRepository.cs`, `BursaryReadRepository.cs` (add `GetBySlugAsync`)
- `tests/GetABursary.Api.Tests/Bursaries/Detail/**`

## Requirements
1. `BursaryDetailResponse`: every field of the summary plus `summary`, `valueSummary`, `coverage`, `workBackRequired`, `workBackDetails`, `minSubjectRequirements`, `citizenship`, `provinces`, `fields[] {code,name}`, `qualifications[] {id,name,institutionShortName}`, `institutions[] {id,shortName}`, `openDate`, `applyUrl` (falls back to the most recently seen source URL if null), `updatedAt`.
2. Unknown or unpublished slug → 404 `Bursary.NotFound`.
3. Closed → 410 ProblemDetails `Bursary.Closed` with `extensions["bursary"]` = the `BursarySummaryResponse`.
4. Add `ErrorType.Gone` mapping usage (already in `ResultExtensions`). Use the `Error.Gone` helper; extend `ResultExtensions` only if needed to attach the extension data (allowed: `src/GetABursary.Api/Extensions/ResultExtensions.cs`).

## Acceptance criteria
- [ ] Integration tests: open → 200 with fields/qualifications/sources; closed → 410 with summary in extensions and **no** eligibility details; unpublished → 404; unknown → 404
- [ ] Service unit tests for each path
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Bursaries.Detail"
```
