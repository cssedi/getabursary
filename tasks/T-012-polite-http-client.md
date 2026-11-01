---
id: T-012
title: Polite HTTP page fetcher with robots.txt, rate limiting and retries
phase: 2
area: worker
model: sonnet
status: todo
depends_on: [T-001, T-003]
requirements: [FR-27, NFR-05]
issue: null
---

## Goal
One shared, well-behaved way to fetch pages: it obeys `robots.txt`, waits between requests to the same domain, identifies itself honestly, and retries transient failures. Every source adapter uses it.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §2 (Result) and §7 (scraper etiquette)
- `specs/01-architecture.md` §2.1 (`Sources/Http/`), §4 (resilience)

## Scope
You may create or modify **only**:
- `src/GetABursary.Worker/Sources/Http/**`
- `src/GetABursary.Worker/Program.cs` (register services), `src/GetABursary.Worker/appsettings.json`, `src/GetABursary.Worker/GetABursary.Worker.csproj`
- `Directory.Packages.props` (add `Microsoft.Extensions.Http.Resilience`)
- `tests/GetABursary.Worker.Tests/Sources/Http/**`

## Requirements
1. `IPageFetcher.GetAsync(Uri url, int minDelayMs, CancellationToken ct) → Task<Result<FetchedPage>>`, where `FetchedPage(Uri Url, int StatusCode, string Html, DateTimeOffset FetchedAt)`.
2. `PoliteHttpClient : IPageFetcher` (typed `HttpClient`):
   - `User-Agent` from config `Ingestion:UserAgent` (default `GetABursaryBot/1.0 (+https://getabursary.co.za/bot)`)
   - **robots.txt:** fetched once per host per process lifetime (cache 6 h), parsed by `RobotsTxtParser`. Supports `User-agent` groups (`GetABursaryBot` group wins over `*`), `Allow`/`Disallow` longest-prefix match, `*` wildcards and `$` end anchors, and `Crawl-delay`. A missing robots.txt (404) means everything is allowed. A 5xx or timeout means **disallow** for this run
   - disallowed → `Result` failure `Fetch.DisallowedByRobots`
   - **per-host delay:** at least `max(minDelayMs, crawl-delay)` between request starts to the same host, enforced with an injectable `IDelayer` (so tests don't really sleep)
   - retries: 3 attempts with exponential backoff on 408, 429, 5xx and network errors (standard resilience handler). A 404 → `Fetch.NotFound` failure (no retry)
   - responses over 2 MB → `Fetch.TooLarge`
3. No live network in tests. Use a fake `HttpMessageHandler`.

## Acceptance criteria
- [ ] `RobotsTxtParserTests`: ≥ 8 cases (specific UA group beats `*`, longest match wins, Allow overrides Disallow at equal length, wildcard, `$`, empty Disallow, crawl-delay, comments)
- [ ] `PoliteHttpClientTests`: disallowed URL is never requested; robots 404 → allowed; per-host delay is requested from `IDelayer` with the right duration; different hosts don't delay each other; UA header is sent; 404 → `Fetch.NotFound`
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Sources.Http"
```
