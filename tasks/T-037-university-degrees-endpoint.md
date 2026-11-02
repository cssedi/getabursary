---
id: T-037
title: One request for a university's degrees (replace per-faculty calls on home page)
phase: 3
area: api
model: haiku
status: todo
depends_on: [T-033]
requirements: [FR-01, NFR-01, NFR-03]
issue: 66
---

## Goal
Picking a university on the home page makes **one** API request instead of one per faculty (review finding on PR #63).

## Context
- `AGENTS.md` (incl. "smallest complete change"), `specs/03-conventions.md`
- Existing: `TaxonomyController`/`TaxonomyService`/`TaxonomyRepository` (T-020); `src/web/src/app/core/api/` `TaxonomyApi`; `src/web/src/app/features/home/home.page.ts` (L150-166 make the per-faculty calls)

## Scope
You may create or modify **only**:
- `src/GetABursary.Api/Controllers/TaxonomyController.cs`, `src/GetABursary.Api/Services/Taxonomy/**`, `src/GetABursary.Api/DTOs/Taxonomy/**`
- `src/GetABursary.Core/Repositories/Taxonomy/ITaxonomyRepository.cs`, `TaxonomyRepository.cs`
- `tests/GetABursary.Api.Tests/Taxonomy/**`
- `src/web/src/app/core/api/**` (add one method), `src/web/src/app/features/home/**`

## Requirements
1. `GET /api/v1/taxonomy/institutions/{id:guid}/qualifications` → faculties with their qualifications: `[{ id, name, slug, qualifications: QualificationResponse[] }]`, sorted by faculty name then qualification name, using the existing `QualificationResponse`. One SQL query (or two at most), `AsNoTracking`, same `[ResponseCache(Duration = 3600)]` as the other taxonomy endpoints. Unknown institution → 404 `Taxonomy.InstitutionNotFound` (reuse the existing error).
2. `TaxonomyApi.qualificationsByInstitution(id)` in the web client.
3. Home page uses it: one call per university pick, which feeds the existing `<optgroup>` per faculty. Delete the per-faculty loop and its `// shortcut:` comment.
4. Keep the existing per-faculty endpoint (the search filter panel may use it).

## Acceptance criteria
- [ ] API integration test: grouped, sorted result for a seeded institution; unknown id → 404
- [ ] Web test: picking a university triggers exactly one HTTP request (`HttpTestingController`)
- [ ] `dotnet build`/`dotnet test` and `npm test -- --watch=false`/`npm run lint`/`npm run build` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Taxonomy"
cd src/web && npm ci && npm test -- --watch=false && npm run build
```
