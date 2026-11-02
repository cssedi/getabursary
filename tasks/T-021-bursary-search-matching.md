---
id: T-021
title: Bursary search endpoint with degree-matching service
phase: 3
area: api
model: sonnet
status: done
depends_on: [T-003, T-006, T-020]
requirements: [FR-01, FR-02, FR-03, FR-04, FR-05, FR-06, NFR-03]
issue: 21
---

## Goal
`GET /api/v1/bursaries/search` returns the bursaries a student is eligible for, matched by qualification/field, institution, study level, marks and dates, ranked by match quality, with source icons. This is the core of the product.

## Context
- `AGENTS.md`, `specs/03-conventions.md`
- `specs/00-product-brief.md` §4.1, §7
- `specs/01-architecture.md` §3.1
- **`specs/02-data-model.md` §5 (matching algorithm: implement exactly)**, §2.1–2.2

## Scope
You may create or modify **only**:
- `src/GetABursary.Api/Controllers/BursariesController.cs`
- `src/GetABursary.Api/Services/Bursaries/IBursaryMatchingService.cs`, `BursaryMatchingService.cs`, `BursarySearchValidator.cs`
- `src/GetABursary.Api/DTOs/Bursaries/**`, `src/GetABursary.Api/DTOs/Common/PagedResponse.cs`
- `src/GetABursary.Api/Extensions/BursaryServiceCollectionExtensions.cs`, `src/GetABursary.Api/Program.cs` (one line)
- `src/GetABursary.Core/Repositories/Bursaries/IBursaryReadRepository.cs`, `BursaryReadRepository.cs`, `BursarySearchCriteria.cs`, `BursarySearchRow.cs`
- `src/GetABursary.Core/Repositories/Taxonomy/**` (add "resolve qualification → field ids + parents + institution" if missing)
- `tests/GetABursary.Api.Tests/Bursaries/Search/**`

## Requirements
1. Query parameters: `qualificationId`, `fieldIds` (repeatable), `institutionId`, `studyLevel`, `average`, `closingFrom`, `closingTo`, `includeClosed` (default false), `sort` (`BestMatch` default | `ClosingSoonest` | `Newest`), `page` (default 1), `pageSize` (default 20, max 50).
2. Validation → 400: `average` 0–100, `pageSize` 1–50, `page` ≥ 1, `closingFrom ≤ closingTo`, unknown `qualificationId` → 400 `Search.UnknownQualification`.
3. The service resolves the criteria (spec §5 "Resolution"). The repository runs **one** database query (EF Core LINQ, or a raw SQL `FromSql` if clearer) that applies every eligibility filter, computes the score as in spec §5 and sorts. Paging is done in SQL.
4. Response: `PagedResponse<BursarySummaryResponse> { items, page, pageSize, totalCount }`. `BursarySummaryResponse` exactly as in spec §5 "Output". `isClosingSoon` uses `IClock`.
5. Add an index if your query needs one (document it in the PR; any migration goes in Core and counts as this task's single migration).

## Acceptance criteria
- [ ] Integration tests with a hand-built dataset (≥ 8 bursaries covering every rule): a qualification match ranks above a field match, which ranks above all-fields; institution restriction excludes others; `average` excludes higher minimums; unknown levels are included but rank lower; closed bursaries are hidden by default and shown last with `includeClosed=true`; date-range filter; paging + `totalCount`; sources listed with icons
- [ ] Validation tests for each 400 case
- [ ] Logged SQL (in a test) shows a single query for the search (no N+1)
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Bursaries.Search"
```
