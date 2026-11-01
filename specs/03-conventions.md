# 03 — Engineering Conventions

> Status: **Accepted v1.0**. Every agent must follow this document. `AGENTS.md` summarises it; this file is the full reference.
> It builds on the Controller → Service → Repository pattern from `ruru-api`, with the fixes listed in [00-product-brief, Appendix A](00-product-brief.md#appendix-a--lessons-from-ruru-api-to-carry-into-conventions).

## 1. Layering {#layering}

```
Controller  ──calls──▶  Service  ──calls──▶  Repository  ──uses──▶  DbContext
   (HTTP)              (rules, mapping)        (data access only)
```

| Layer | May | Must not |
|---|---|---|
| **Controller** (`Api/Controllers`) | Bind and validate the request DTO, read the current user, call **one** service method, convert `Result<T>` to HTTP with `ToActionResult()` | Contain business logic, touch the `DbContext` or repositories, return entities |
| **Service** (`Api/Services/<Feature>`, `Worker/...`) | Hold business rules, orchestrate repositories, map DTO ↔ entity, return `Result<T>` | Know about HTTP (`IActionResult`, status codes, `HttpContext`) |
| **Repository** (`Core/Repositories/<Feature>`) | Run EF Core queries and commands for one aggregate, return entities or projections | Take request/response DTOs, contain business rules, call other repositories |

- Every service and repository has an interface (`IBursaryService` / `BursaryService`) and is registered as **scoped** in a `ServiceCollectionExtensions` method per feature (`AddBursaryFeature()`), not inline in `Program.cs`.
- Use constructor injection. Prefer primary constructors: `public class BursaryService(IBursaryRepository repository) : IBursaryService`.
- One `SaveChangesAsync` per use case, called by the **service** through the repository (`repository.SaveChangesAsync(ct)`).

## 2. `Result<T>` and errors

```csharp
public enum ErrorType { Validation, NotFound, Conflict, Gone, Unauthorized, Forbidden, Unexpected }
public sealed record Error(string Code, string Message, ErrorType Type);   // Code e.g. "Bursary.NotFound"

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<Error> Errors { get; }
    public static Result<T> Success(T value);
    public static Result<T> Failure(params Error[] errors);
}
```

- Services return `Result<T>` for **expected** failures (not found, validation, conflict). Throw exceptions only for unexpected failures. Global exception handling returns a `500` ProblemDetails.
- `ResultExtensions.ToActionResult()` maps `Validation→400`, `NotFound→404`, `Conflict→409`, `Gone→410`, `Unauthorized→401`, `Forbidden→403`, `Unexpected→500`. Failures are returned as RFC 7807 `ProblemDetails`, with `errors` in `extensions`.
- Error definitions live in static classes per feature: `public static class BursaryErrors { public static Error NotFound(Guid id) => new("Bursary.NotFound", $"Bursary {id} was not found", ErrorType.NotFound); }`

## 3. C# style
- `Directory.Build.props`: `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<AnalysisLevel>latest-recommended</AnalysisLevel>`.
- File-scoped namespaces that match folders: `GetABursary.Api.Services.Bursaries`.
- Folders and namespaces are **PascalCase** (a change from ruru-api's lowercase folders).
- DTOs are `record`s with `init` properties, named `<Thing>Request` / `<Thing>Response`.
- All I/O methods are `async`, end in `Async`, and accept a `CancellationToken ct` as their last parameter.
- New IDs: `Guid.CreateVersion7()`. Time comes from the injected `IClock` (`UtcNow`), never `DateTime.Now`, so tests can control it.
- Mapping is done by hand in the service (or a static `ToResponse()` extension). No AutoMapper.
- Read queries use `AsNoTracking()` and project to only the columns needed.

## 4. EF Core & database
- Configuration lives in `IEntityTypeConfiguration<T>` classes in `Core/Data/Configurations`. No data annotations on entities except `[MaxLength]` where helpful.
- Migrations: `dotnet ef migrations add <PascalCaseName> -p src/GetABursary.Core -s src/GetABursary.Api`. **One migration per task** that changes the schema, with a descriptive name (e.g. `AddTaxonomyTables`). Never edit a migration that has already been merged.
- Seed data is loaded by an idempotent `DataSeeder` from `data/seed/*.json`, not by `HasData`. (Exception: enum-based Identity roles may be ensured at startup, as in ruru-api.)

## 5. Testing
- Frameworks: **xUnit** with the built-in `Assert`, **NSubstitute** for mocks, **Testcontainers.PostgreSql** for integration tests. (No FluentAssertions; v8 requires a paid licence.)
- Naming: `MethodName_Scenario_ExpectedResult`.
- Each task's acceptance criteria name the tests it must add. Expected coverage:
  - Services: unit tests with mocked repositories (success and each failure path)
  - Repositories: integration tests against a real Postgres container
  - Controllers/endpoints: at least one `WebApplicationFactory` integration test per endpoint
  - Source adapters: tests against saved HTML in `tests/GetABursary.Worker.Tests/Fixtures/<sourceKey>/`, with **no live network calls in tests**
- `dotnet test` must pass locally and in CI.

## 6. Git, branches, PRs
- Branches: `task/T-012-polite-http-client`.
- Commits follow Conventional Commits: `feat(worker): add polite http client (T-012)`.
- **One task = one PR.** The PR description includes `Closes #<issue>`, a checklist copied from the task's acceptance criteria, and the commands run to verify.
- The PR also updates the task file's front-matter to `status: done`.
- Stay inside the task's **Scope** list of files. If you need to change something outside it, stop and comment on the issue (see [04-agent-workflow](04-agent-workflow.md#escalation)).

## 7. Security & privacy
- Never commit secrets, real connection strings, API keys or personal data. Use `.env.example` with placeholder values.
- Validate all input (lengths, enums, ranges). Rate-limit anonymous write endpoints.
- Store `ip_hash` (salted SHA-256), never raw IP addresses (POPIA).
- Scrapers: obey `robots.txt`, keep at least the source's `crawl_delay_ms` between requests, and send `User-Agent: GetABursaryBot/1.0 (+https://getabursary.co.za/bot)`.

## 8. Angular (summary; expanded when Phase 3 tasks are written)
- Standalone components, signals, the new control-flow syntax, SSR enabled.
- Features go in `src/web/src/app/features/<feature>/`. Shared UI goes in `shared/ui/`. API clients go in `core/api/`.
- Strictly typed API models that mirror the API DTOs.
- Must work on a 360 px wide screen first (NFR-01). No heavy UI library without an ADR.
