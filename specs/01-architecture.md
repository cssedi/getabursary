# 01 — Architecture

> Status: **Accepted v1.0**. Implements: [00-product-brief](00-product-brief.md). Decisions: [ADR-001](adr/ADR-001-postgres-only.md), [ADR-002](adr/ADR-002-agent-workflow.md).

## 1. System overview

```
                         ┌────────────────────────────────────────┐
  Student (mobile web) ─▶│ Web: Angular + SSR  (src/web)          │
  Admin                  └───────────────┬────────────────────────┘
                                         │ HTTPS / JSON  (/api/v1/...)
                         ┌───────────────▼────────────────────────┐
                         │ API: ASP.NET Core  (GetABursary.Api)   │
                         │ Controller → Service → Repository      │
                         └───────────────┬────────────────────────┘
                                         │ EF Core (Npgsql)
                         ┌───────────────▼────────────────────────┐
                         │ PostgreSQL 17                          │
                         └───────────────▲────────────────────────┘
                                         │ EF Core (Npgsql)
                         ┌───────────────┴────────────────────────┐
  Aggregator sites ◀─────│ Worker: .NET Worker Service            │──▶ LLM API (extraction)
   (HTTP, robots-aware)  │ (GetABursary.Worker)                   │
                         │ Scheduler → Adapters → Snapshot →      │
                         │ Parse → Extract → Dedupe → Publish     │
                         └────────────────────────────────────────┘
```

There are three deployable units: **web**, **api** and **worker**. They share **one database**. The API and the worker never call each other. They communicate only through the database (e.g. the admin's "Run now" button inserts an `IngestionRunRequest` row, and the worker polls for it).

## 2. Repository layout

```
getabursary/
├── AGENTS.md                  # rules for every agent (read first)
├── CLAUDE.md                  # points to AGENTS.md
├── GetABursary.slnx           # solution (.NET 10 slnx format)
├── global.json                # pins the .NET SDK
├── Directory.Build.props      # shared C# settings (nullable, warnings as errors, analyzers)
├── docker-compose.yml         # local dev: postgres
├── specs/                     # product + technical specs (source of truth)
│   └── adr/                   # architecture decision records
├── tasks/                     # one markdown file per agent task (mirrored to GitHub Issues)
├── data/seed/                 # reference data (JSON): fields of study, institutions, ...
├── scripts/                   # repo tooling (issue sync, etc.)
├── src/
│   ├── GetABursary.Core/      # domain entities, DbContext, repositories, Result<T>  (class library)
│   ├── GetABursary.Api/       # controllers, services, DTOs, auth                    (web api)
│   ├── GetABursary.Worker/    # ingestion pipeline, source adapters, scheduler       (worker service)
│   └── web/                   # Angular app (SSR)
└── tests/
    ├── GetABursary.Core.Tests/
    ├── GetABursary.Api.Tests/      # unit + integration (WebApplicationFactory + Testcontainers)
    └── GetABursary.Worker.Tests/   # adapter tests against saved HTML fixtures
```

### 2.1 Project internals (Controller → Service → Repository)

```
GetABursary.Core/
├── Common/            Result.cs, Error.cs, ErrorType.cs, Clock (IClock)
├── Domain/            entities grouped by feature: Taxonomy/, Bursaries/, Ingestion/, Reports/, Users/
├── Data/              GetABursaryDbContext.cs, Configurations/ (IEntityTypeConfiguration per entity), Migrations/, Seeding/
└── Repositories/      <Feature>/I<Name>Repository.cs + <Name>Repository.cs

GetABursary.Api/
├── Controllers/       thin: bind request → call service → map Result<T> to HTTP
├── Services/          <Feature>/I<Name>Service.cs + <Name>Service.cs  (business rules, DTO mapping)
├── DTOs/              <Feature>/<Name>Request.cs, <Name>Response.cs  (records)
├── Extensions/        ResultExtensions (Result → IActionResult), ServiceCollectionExtensions
└── Program.cs

GetABursary.Worker/
├── Scheduling/        IngestionScheduler (BackgroundService), cron config
├── Pipeline/          IngestionPipeline, stages (Snapshot, Extract, Dedupe, Publish)
├── Sources/           ISourceAdapter, SourceAdapterBase, Http/ (PoliteHttpClient, RobotsTxt), Adapters/<SourceName>/
├── Extraction/        IEligibilityExtractor, LlmEligibilityExtractor, FakeEligibilityExtractor
└── Program.cs
```

Layer rules are in [03-conventions.md](03-conventions.md#layering).

## 3. Key flows

### 3.1 Search (FR-01–FR-04)
1. Web calls `GET /api/v1/bursaries/search?qualificationId=…&level=…&average=…&includeClosed=false&page=1`.
2. `BursariesController` calls `IBursaryMatchingService.SearchAsync(BursarySearchRequest)`.
3. The service resolves the qualification to its fields of study (`ITaxonomyRepository`), builds the criteria, and calls `IBursaryRepository.SearchAsync(criteria)`. The repository runs a single SQL query. Scoring is done in SQL (see [02-data-model §5](02-data-model.md#5-matching-algorithm)).
4. The service maps entities to `BursarySummaryResponse` (including the source icons) and returns `Result<PagedResponse<…>>`.

### 3.2 Ingestion (FR-20–FR-27)
```
for each enabled Source:
  adapter.DiscoverAsync()          → list of listing URLs              (respect robots.txt, rate limit)
  for each url:
    html = fetcher.GetAsync(url)
    snapshot = SnapshotStore.Save(sourceId, url, html)   → contentHash
    if snapshot unchanged since last run: mark seen, skip  (FR-25)
    raw = adapter.Parse(html)       → RawListing (title, funder?, closingDate?, rawEligibilityText, applyUrl?)
    extracted = extractor.Extract(raw) → ExtractedEligibility (+ confidence)   (FR-23)
    BursaryMerger.Upsert(raw, extracted)  → match existing canonical Bursary or create one (FR-24)
       - fields where Bursary.ManuallyLocked = true are never overwritten (FR-42)
       - confidence < 0.7 or new funder → ReviewItem(NeedsReview)
after all sources:
  ClosureJob: closingDate < today → Status=Closed (FR-26)
  VanishedJob: listings not seen in any source for 2 consecutive runs → ReviewItem(Vanished)
  IngestionRun row records counts/errors per source (FR-27, NFR-05)
```
A failure in one adapter is caught, logged on that source's `IngestionRunSource` row, and the run moves on to the next source.

### 3.3 Saves, tracker and the sign-in prompt (FR-09, FR-11–FR-14)
- Search and filter state lives in the **URL query string** (`/search?qualification=…&level=…&average=…`). It isn't stored server-side or in `localStorage`.
- Saved bursaries, tracker entries and the profile are stored **only via the API** (`/api/v1/me/*`, requires the `Student` role). There's no anonymous local storage, so nothing needs merging.
- **Sign-in prompt flow (web):** a `requireAuth(action)` helper wraps save/track/profile actions. If signed out, it stores the pending action (e.g. `{ type: 'save', bursaryId }`) in memory or `sessionStorage`, opens the sign-in dialog, and replays the action after a successful sign-in (including after the Google redirect).

## 4. Technology choices

| Concern | Choice | Notes |
|---|---|---|
| Runtime | .NET 10 (LTS) | Pinned via `global.json` |
| API | ASP.NET Core controllers | Built-in OpenAPI; Swagger UI in Development only |
| ORM | EF Core 10 + Npgsql | Migrations live in Core; snake_case naming via `EFCore.NamingConventions` |
| DB | PostgreSQL 17 | `pg_trgm` for fuzzy dedupe of names; `pgvector` reserved for later |
| Auth | ASP.NET Core Identity + JWT bearer; Google sign-in | Roles: `Student`, `Admin` (enum-seeded, as in ruru-api) |
| Worker scheduling | `BackgroundService` + `Cronos` cron parsing | Default `0 3 * * *` (03:00 SAST) |
| HTML parsing | AngleSharp | Playwright only if a source needs JavaScript (decided per adapter) |
| HTTP resilience | `Microsoft.Extensions.Http.Resilience` | Retry + per-domain rate limiter |
| LLM extraction | Anthropic Messages API via `HttpClient` (small model) | Behind `IEligibilityExtractor`; a fake implementation is used in tests |
| Tests | xUnit, Testcontainers.PostgreSql, `WebApplicationFactory` | No paid assertion libraries |
| Web | Angular (latest stable), standalone components, signals, SSR | Detailed in the Phase 3 specs |
| CI | GitHub Actions | `.github/workflows/ci.yml` |
| Hosting (MVP) | One VPS with Docker Compose + Caddy | AWS path described in brief §9 |

## 5. Configuration & secrets
- All config goes through `appsettings.json` plus environment variables. **No secrets are committed** (public repo, NFR-07).
- Locally, use `dotnet user-secrets` or a `.env` file (git-ignored). Only `.env.example` is committed.
- Named settings: `ConnectionStrings:Default`, `Jwt:*`, `Auth:Google:*`, `Extraction:Provider`, `Extraction:ApiKey`, `Extraction:Model`, `Ingestion:Cron`, `Ingestion:UserAgent`.

## 6. API surface (v1, MVP)

| Method & path | Auth | Purpose | Req |
|---|---|---|---|
| `GET /api/v1/health` | – | liveness | – |
| `GET /api/v1/taxonomy/institutions` | – | list institutions | FR-01 |
| `GET /api/v1/taxonomy/institutions/{id}/faculties` | – | faculties | FR-01 |
| `GET /api/v1/taxonomy/faculties/{id}/qualifications` | – | qualifications | FR-01 |
| `GET /api/v1/taxonomy/fields` | – | fields of study | FR-01 |
| `GET /api/v1/bursaries/search` | – | matching search | FR-01–FR-04 |
| `GET /api/v1/bursaries/{slug}` | – | detail (closed → `410 Gone` + summary) | FR-06, FR-07 |
| `POST /api/v1/bursaries/{id}/reports` | – (rate limited) | report an inaccuracy | FR-08 |
| `GET/POST/PATCH/DELETE /api/v1/me/tracker` | Student | synced tracker | FR-12–FR-14 |
| `/api/v1/admin/...` | Admin | review queue, bursaries, sources, taxonomy | FR-40–FR-44 |

## 7. Non-functional notes
- **Rate limiting** (ASP.NET Core rate limiter) on the anonymous POST endpoints.
- **CORS:** an explicit allow-list of web origins, not `AllowAnyOrigin`, in production.
- **Logging:** structured `ILogger`. Ingestion runs are also stored in the database for the admin screen.
- **SEO (NFR-02):** Angular SSR renders `/bursaries/:slug` and `/bursaries/field/:field`, with a sitemap generated from the API.
