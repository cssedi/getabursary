---
id: T-009
title: Implement idempotent DataSeeder for reference JSON
phase: 1
area: core
model: haiku
status: todo
depends_on: [T-006, T-008]
requirements: [FR-30, FR-32]
issue: 9
---

## Goal
Reference data from `data/seed/*.json` is upserted into the database on startup, so every environment has the same fields of study, institutions and sources. It's safe to run any number of times.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §4 (seed data rule)
- `specs/02-data-model.md` §2.1, §2.2 (`source`), §3
- `data/seed/README.md`

## Scope
You may create or modify **only**:
- `src/GetABursary.Core/Data/Seeding/**`
- `src/GetABursary.Core/DependencyInjection.cs` (register the seeder)
- `src/GetABursary.Worker/Program.cs` (run the seeder at startup)
- `src/GetABursary.Api/Program.cs` (run the seeder at startup in Development only, after migrations)
- `src/GetABursary.Core/GetABursary.Core.csproj`, `src/GetABursary.Worker/GetABursary.Worker.csproj`, `src/GetABursary.Api/GetABursary.Api.csproj` (copy `data/seed/**` to the output as `seed/**`)
- `tests/GetABursary.Core.Tests/Data/Seeding/**`

## Requirements
1. `IDataSeeder.SeedAsync(CancellationToken ct)` and `DataSeeder` read `fields-of-study.json`, `institutions.json`, `sources.json` from a configurable directory (`Seeding:Path`, default `<AppContext.BaseDirectory>/seed`). Also load `faculties/*.json` if present, upserting faculties by (institution, slug) and qualifications by (faculty, slug) and replacing their field links. The format is defined in `tasks/T-010-faculties-and-qualifications-seed.md`; skip if the folder doesn't exist. Cover it with a test using a small temp faculties file.
2. Upsert keys: field by `code`, institution by `slug`, source by `key`. Existing rows get their name, URLs and parent updated. **Never delete** rows. **Never overwrite `source.enabled`** once the row exists (admins control it).
3. Fields are inserted parents first.
4. Log counts (inserted, updated) per file.

## Acceptance criteria
- [ ] Integration test: running against an empty DB inserts 20 fields, 26 institutions, 9 sources
- [ ] Integration test: a second run inserts 0 and updates 0 (idempotent)
- [ ] Integration test: changing a name in a temp seed copy and re-running updates that row only
- [ ] Integration test: `source.enabled` set to true in the DB stays true after re-seeding
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Seeding"
```
