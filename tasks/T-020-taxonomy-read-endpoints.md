---
id: T-020
title: Taxonomy read endpoints (institutions, faculties, qualifications, fields)
phase: 3
area: api
model: haiku
status: done
depends_on: [T-003, T-009]
requirements: [FR-01]
issue: 20
---

## Goal
The web app can fill the University → Faculty → Qualification pickers and the field-of-study filter from the API.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §1–§3 (layering, Result, style)
- `specs/01-architecture.md` §3.1, §6 (API surface rows for `/taxonomy`)
- `specs/02-data-model.md` §2.1

## Scope
You may create or modify **only**:
- `src/GetABursary.Api/Controllers/TaxonomyController.cs`
- `src/GetABursary.Api/Services/Taxonomy/**`, `src/GetABursary.Api/DTOs/Taxonomy/**`
- `src/GetABursary.Api/Extensions/TaxonomyServiceCollectionExtensions.cs`, `src/GetABursary.Api/Program.cs` (one line: `AddTaxonomyFeature()`)
- `src/GetABursary.Core/Repositories/Taxonomy/ITaxonomyRepository.cs`, `TaxonomyRepository.cs` only (do not touch `FieldOfStudyRepository`, which belongs to T-017)
- `tests/GetABursary.Api.Tests/Taxonomy/**`

## Requirements
1. Endpoints (all `GET`, anonymous, `[ResponseCache(Duration = 3600)]`):
   - `/api/v1/taxonomy/institutions` → `InstitutionResponse[] { id, name, shortName, slug, type, province }`, sorted by name. By default return **only institutions that have at least one qualification** (brief FR-32: the picker shows the big 5 in the MVP). `?includeAll=true` returns all of them
   - `/api/v1/taxonomy/institutions/{id:guid}/faculties` → `FacultyResponse[] { id, name, slug }`. Unknown institution → 404 `Taxonomy.InstitutionNotFound`
   - `/api/v1/taxonomy/faculties/{id:guid}/qualifications` → `QualificationResponse[] { id, name, abbreviation, studyLevel, slug, fieldCodes[] }`. Unknown faculty → 404
   - `/api/v1/taxonomy/fields` → `FieldOfStudyResponse[] { id, code, name, slug, parentCode, children[] }` as a tree (first order with nested second order)
2. Controller → `ITaxonomyService` → `ITaxonomyRepository` (projections with `AsNoTracking`), returning `Result<T>` and using `ToActionResult()`.

## Acceptance criteria
- [ ] Service unit tests (mocked repository): not-found paths return the right error codes
- [ ] Integration tests (`WebApplicationFactory` + Postgres container + seeder): each endpoint returns 200 with seeded data; unknown IDs → 404 ProblemDetails
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Taxonomy"
```
