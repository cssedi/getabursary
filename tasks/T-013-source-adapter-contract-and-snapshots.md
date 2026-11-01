---
id: T-013
title: Source adapter contract, adapter registry and snapshot store
phase: 2
area: worker
model: sonnet
status: todo
depends_on: [T-007, T-012]
requirements: [FR-21, FR-22, FR-25]
issue: null
---

## Goal
A stable contract that every aggregator adapter implements, a registry to look adapters up by source key, and a snapshot store that saves fetched HTML and reports whether the content changed. Adapter tasks T-014–T-016 build on this.

## Context
- `AGENTS.md`, `specs/03-conventions.md`
- `specs/01-architecture.md` §2.1, §3.2 (pipeline pseudo-code)
- `specs/02-data-model.md` §2.3 (`raw_listing`)

## Scope
You may create or modify **only**:
- `src/GetABursary.Worker/Sources/ISourceAdapter.cs`, `RawListing.cs`, `SourceAdapterRegistry.cs`, `SourceAdapterBase.cs`, `Parsing/**`
- `src/GetABursary.Worker/Pipeline/SnapshotStore.cs`, `ISnapshotStore.cs`
- `src/GetABursary.Core/Repositories/Ingestion/IRawListingRepository.cs`, `RawListingRepository.cs`
- `src/GetABursary.Worker/Program.cs` (registration), `src/GetABursary.Worker/GetABursary.Worker.csproj`, `Directory.Packages.props` (add `AngleSharp`)
- `tests/GetABursary.Worker.Tests/Sources/**` (excluding `Adapters/`), `tests/GetABursary.Worker.Tests/Pipeline/**`, `tests/GetABursary.Core.Tests/Repositories/Ingestion/**`

## Requirements
1. ```csharp
   public interface ISourceAdapter
   {
       string SourceKey { get; }                         // matches source.key, e.g. "zabursaries"
       IAsyncEnumerable<Uri> DiscoverAsync(IPageFetcher fetcher, SourceOptions options, CancellationToken ct);
       Result<RawListing> Parse(Uri url, string html);
   }
   public sealed record SourceOptions(Uri BaseUrl, int CrawlDelayMs, int MaxPages = 20, int MaxListings = 500);
   public sealed record RawListing(
       Uri SourceUrl, string Title, string? FunderName, DateOnly? ClosingDate, DateOnly? OpenDate,
       Uri? ApplyUrl, string EligibilityText, string? FieldsHint, string? ValueText);
   ```
2. `SourceAdapterBase` gives helpers that adapters reuse: `ParseDocument(html)` (AngleSharp), `CleanText(element)` (collapse whitespace, strip scripts/styles), and `Truncate(text, 8000)`.
3. `Parsing/SaDateParser.TryParse(string text, out DateOnly date)`: handles `31 March 2027`, `31 Mar 2027`, `31/03/2027`, `2027-03-31`, `March 31, 2027`, and phrases like "Closing date: 31 March 2027". Use the invariant culture with English month names.
4. `SourceAdapterRegistry`: all `ISourceAdapter`s from DI. `Get(string key)` returns `Result<ISourceAdapter>` (`Source.AdapterNotFound`). Duplicate keys throw at startup.
5. `SnapshotStore.SaveAsync(Guid sourceId, Uri url, string html, ct) → SnapshotResult(Guid RawListingId, bool Changed)`: SHA-256 hex of the normalised HTML (strip whitespace runs, and also `<script>` blocks so ad/analytics churn doesn't count as a change). Upsert by (`source_id`, `url`), update `last_fetched_at`, and set `last_changed_at` + `html` only when the hash differs. `missed_runs = 0`.

## Acceptance criteria
- [ ] `SaDateParserTests`: every format above, plus invalid input returns false
- [ ] `SourceAdapterRegistryTests`: lookup works, unknown key fails, duplicate key throws
- [ ] Integration tests for `SnapshotStore`: first save → `Changed=true`; same HTML → `Changed=false`; HTML differing only in script contents/whitespace → `Changed=false`; real change → `Changed=true`
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Sources|FullyQualifiedName~Pipeline|FullyQualifiedName~Repositories.Ingestion"
```
