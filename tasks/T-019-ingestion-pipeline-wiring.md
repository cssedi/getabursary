---
id: T-019
title: Wire the ingestion pipeline end to end, plus closure and vanished jobs
phase: 2
area: worker
model: sonnet
status: done
depends_on: [T-011, T-018]
requirements: [FR-20, FR-22, FR-25, FR-26, FR-27, NFR-05]
issue: 19
---

## Goal
A scheduled or manual run walks every enabled source through Discover → Fetch → Snapshot → Parse → Extract → Merge, records per-source stats, closes expired bursaries, and flags listings that vanished.

## Context
- `AGENTS.md`, `specs/03-conventions.md`
- `specs/01-architecture.md` §3.2 (implement this pseudo-code)
- `specs/02-data-model.md` §2.3, §2.4
- Existing: `IIngestionPipeline` (T-011), `ISourceAdapter`/`SnapshotStore` (T-013), `IPageFetcher` (T-012), `IEligibilityExtractor` (T-017), `BursaryMerger` (T-018)

## Scope
You may create or modify **only**:
- `src/GetABursary.Worker/Pipeline/IngestionPipeline.cs`, `Pipeline/Jobs/**`
- `src/GetABursary.Worker/Program.cs` (replace `NoOpIngestionPipeline` registration; delete the NoOp class)
- `src/GetABursary.Core/Repositories/Ingestion/**`, `src/GetABursary.Core/Repositories/Bursaries/**` (add the methods needed)
- `tests/GetABursary.Worker.Tests/Pipeline/**`

## Requirements
1. `IngestionPipeline.RunAsync(runId, sourceId?, ct)`:
   - load enabled sources (or just `sourceId`). For each: get the adapter from the registry (missing adapter → log + count as failed, continue)
   - per listing URL: fetch (`crawl_delay_ms`) → snapshot. **Unchanged → mark seen** (`bursary_source.last_seen_at`, `raw_listing.missed_runs = 0`), count `unchanged`, skip. Changed → parse → extract → merge, storing `parsed`/`extracted` JSON on the raw listing
   - extraction failure → `ReviewItem(LowConfidence)` with the error in `details`, count `failed`
   - every exception inside one listing is caught (count `failed`). Every exception inside one source is caught and stored in `ingestion_run_source.error_summary` (first 2000 chars). The run continues (NFR-05)
   - write an `IngestionRunSource` row per source with all counters
2. `Jobs/ClosureJob`: published bursaries with `closing_date < today (SAST)` → `Closed`.
3. `Jobs/VanishedJob`: for each source processed in this run, raw listings **not** seen this run get `missed_runs + 1`. When `missed_runs` reaches 2 and the bursary has no other source seen in the last 7 days → `ReviewItem(Vanished)` (once).
4. The run status is `PartiallyFailed` if any source failed but at least one succeeded.

## Acceptance criteria
- [ ] Pipeline tests using a fake adapter (2 sources, one throwing), fake fetcher, `FakeEligibilityExtractor` and the Postgres fixture: the good source's bursaries are created; the bad source's error is recorded; run = `PartiallyFailed`
- [ ] A second run with identical HTML: all `unchanged`, no new bursaries, `last_seen_at` updated
- [ ] `ClosureJobTests` and `VanishedJobTests` (vanish after 2 missed runs, once only)
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Pipeline"
```
