---
id: T-005
title: Add taxonomy entities (institution, faculty, qualification, field of study)
phase: 1
area: core
model: haiku
status: done
depends_on: [T-004]
requirements: [FR-30, FR-31]
issue: 5
---

## Goal
The degree map exists in the database: institutions → faculties → qualifications, and qualifications ↔ CESM fields of study.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §3, §4
- `specs/02-data-model.md` §1 (enums `StudyLevel`, `InstitutionType`, `Province`) and §2.1 (taxonomy tables)

## Scope
You may create or modify **only**:
- `src/GetABursary.Core/Domain/Taxonomy/**`, `src/GetABursary.Core/Domain/Common/Enums/**`
- `src/GetABursary.Core/Data/Configurations/Taxonomy/**`
- `src/GetABursary.Core/Data/GetABursaryDbContext.cs` (add `DbSet`s only)
- `src/GetABursary.Core/Data/Migrations/**` (one new migration `AddTaxonomy`)
- `tests/GetABursary.Core.Tests/Data/Taxonomy/**`

## Requirements
1. Enums `StudyLevel`, `InstitutionType`, `Province` in `Domain/Common/Enums/`, with values exactly as in the data-model spec. Store them as strings (`HasConversion<string>()`).
2. Entities (inherit `Entity`): `FieldOfStudy` (Code, Name, Slug, ParentId, Parent, Children), `Institution`, `Faculty`, `Qualification`, plus the join entity `QualificationField` (composite key, **does not** inherit `Entity`).
3. Configurations: required columns, max lengths (names 200, slugs 120, codes 10), and unique indexes exactly as in spec §2.1. Deletes cascade from Institution → Faculty → Qualification. FieldOfStudy uses a restrict delete on its parent.
4. Migration `AddTaxonomy`.

## Acceptance criteria
- [x] Integration tests (Postgres fixture): an institution with a faculty and qualification linked to two fields can be saved and reloaded with `Include`
- [x] Integration test: a duplicate `field_of_study.code` throws `DbUpdateException`
- [x] Integration test: table and column names are snake_case (e.g. query `information_schema.columns` for `qualification_field.field_of_study_id`)
- [x] `dotnet build` (zero warnings) and `dotnet test` pass
- [x] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Data.Taxonomy"
```
