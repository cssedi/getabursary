---
id: T-004
title: Add EF Core DbContext, Postgres wiring and Testcontainers fixture
phase: 0
area: core
model: sonnet
status: todo
depends_on: [T-001, T-002, T-003]
requirements: [NFR-06]
issue: null
---

## Goal
`GetABursaryDbContext` connected to PostgreSQL with snake_case naming, automatic audit timestamps, the `pg_trgm` extension, an initial migration, and a reusable Testcontainers fixture so later tasks can write real-database integration tests.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §4, §5
- `specs/01-architecture.md` §2.1, §4, §5
- `specs/02-data-model.md` (header notes: keys, timestamps, naming)

## Scope
You may create or modify **only**:
- `src/GetABursary.Core/Data/**` (DbContext, design-time factory, `Migrations/`)
- `src/GetABursary.Core/Domain/Common/**` (base entity)
- `src/GetABursary.Core/DependencyInjection.cs`
- `src/GetABursary.Core/GetABursary.Core.csproj`, `Directory.Packages.props`
- `src/GetABursary.Api/Program.cs`, `src/GetABursary.Api/appsettings*.json`
- `src/GetABursary.Worker/Program.cs`, `src/GetABursary.Worker/appsettings*.json`
- `tests/GetABursary.Core.Tests/**`

## Requirements
1. Packages: `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `Microsoft.EntityFrameworkCore.Design` (Api, private assets), `Testcontainers.PostgreSql` (tests).
2. `Domain/Common/Entity.cs`: abstract `Entity` with `Guid Id` (init with `Guid.CreateVersion7()`), `DateTimeOffset CreatedAt`, `DateTimeOffset UpdatedAt`.
3. `Data/GetABursaryDbContext.cs`:
   - constructor takes `DbContextOptions<GetABursaryDbContext>` and `IClock`
   - `OnModelCreating`: `HasPostgresExtension("pg_trgm")`, `ApplyConfigurationsFromAssembly(...)`
   - override `SaveChangesAsync` to set `CreatedAt`/`UpdatedAt` from `IClock` on added/modified `Entity` instances
4. `DependencyInjection.cs`: `AddCoreData(this IServiceCollection, IConfiguration)`. Reads `ConnectionStrings:Default`, calls `UseNpgsql(...).UseSnakeCaseNamingConvention()`, and registers `IClock` if not already registered (`TryAddSingleton`).
5. A design-time factory (`IDesignTimeDbContextFactory`) that reads the connection string from the environment variable `ConnectionStrings__Default` or falls back to the local docker default, so `dotnet ef` works.
6. Migration `InitialCreate` (contains only the extension).
7. Api and Worker call `AddCoreData`. `appsettings.json` has `"ConnectionStrings": { "Default": "" }` (empty). `appsettings.Development.json` uses the local docker defaults from T-002 (password `localdev`, which is not a secret).
8. In **Development only**, the Api applies pending migrations at startup.
9. `tests/GetABursary.Core.Tests/Infrastructure/PostgresFixture.cs`: xUnit `IAsyncLifetime` collection fixture that starts `postgres:17-alpine`, applies migrations, and exposes `CreateContext()`. Add a `[CollectionDefinition("Postgres")]`.

## Acceptance criteria
- [ ] `DbContextTests.Migrations_Apply_And_PgTrgm_Exists` (integration, uses the fixture)
- [ ] `DbContextTests.SaveChanges_Sets_Audit_Timestamps` (uses a test-only entity configured in the test project, or a fake clock)
- [ ] `dotnet ef migrations list -p src/GetABursary.Core -s src/GetABursary.Api` works against the local DB
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass in CI
- [ ] Task file `status: done`

## How to verify
```bash
docker compose up -d db
dotnet tool restore
dotnet ef migrations list -p src/GetABursary.Core -s src/GetABursary.Api
dotnet test --filter "FullyQualifiedName~DbContextTests"
```
