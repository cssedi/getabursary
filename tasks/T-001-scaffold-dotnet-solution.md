---
id: T-001
title: Scaffold .NET solution, projects and health endpoint
phase: 0
area: infra
model: sonnet
status: todo
depends_on: []
requirements: [NFR-06]
issue: 1
---

## Goal
A buildable .NET 10 solution with the project layout from the architecture spec, shared build settings, test projects, and a tested `GET /api/v1/health` endpoint, so CI starts building and testing.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §1, §3, §5
- `specs/01-architecture.md` §2 (repository layout) and §4 (technology choices)

## Scope
You may create or modify **only**:
- `GetABursary.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.config/dotnet-tools.json`
- `src/GetABursary.Core/**`, `src/GetABursary.Api/**`, `src/GetABursary.Worker/**`
- `tests/GetABursary.Core.Tests/**`, `tests/GetABursary.Api.Tests/**`, `tests/GetABursary.Worker.Tests/**`

Out of scope: database, EF Core, Docker, Angular.

## Requirements
1. `global.json`: pin the .NET 10 SDK (`"version": "10.0.100"`, `"rollForward": "latestFeature"`).
2. `Directory.Build.props`: `net10.0`, `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` true, `AnalysisLevel` latest-recommended, `LangVersion` latest.
3. Use **central package management** (`Directory.Packages.props`, `ManagePackageVersionsCentrally=true`).
4. Projects (root namespaces = project names):
   - `src/GetABursary.Core`: class library. Create empty folders `Common/`, `Domain/`, `Data/`, `Repositories/`, each with a `.gitkeep`.
   - `src/GetABursary.Api`: ASP.NET Core Web API using **controllers** (`dotnet new webapi --use-controllers`). Delete the WeatherForecast sample. References Core. Keep built-in OpenAPI plus Swagger UI in Development only (as in ruru-api). Add `public partial class Program;` at the end of `Program.cs` for tests.
   - `src/GetABursary.Worker`: Worker Service (`dotnet new worker`). Delete the sample `Worker.cs` loop and leave an empty host. References Core.
   - `tests/*`: xUnit projects referencing their matching src project. Api.Tests also references `Microsoft.AspNetCore.Mvc.Testing`.
5. `Controllers/HealthController.cs`: route `api/v1/health`, `GET` returns `200 { "status": "ok" }` using a `HealthResponse` record in `DTOs/Health/`.
6. `.config/dotnet-tools.json` with `dotnet-ef` (version 10.x).

## Acceptance criteria
- [ ] `dotnet build` at the repo root succeeds with zero warnings
- [ ] `tests/GetABursary.Api.Tests/Health/HealthEndpointTests.cs` uses `WebApplicationFactory<Program>` and asserts `200` and `status == "ok"`
- [ ] `dotnet test` passes
- [ ] No WeatherForecast or other template leftovers
- [ ] CI (`.github/workflows/ci.yml`) is green on the PR
- [ ] Task file `status: done`

## How to verify
```bash
dotnet build
dotnet test
dotnet run --project src/GetABursary.Api &  sleep 5 && curl -s localhost:5000/api/v1/health
```
