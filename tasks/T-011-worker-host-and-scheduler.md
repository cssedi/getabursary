---
id: T-011
title: Worker host with cron scheduler, manual run requests and run recording
phase: 2
area: worker
model: sonnet
status: todo
depends_on: [T-007]
requirements: [FR-20, FR-27, NFR-05]
issue: null
---

## Goal
The worker starts an ingestion run on a cron schedule (default daily at 03:00 SAST) and whenever an `ingestion_run_request` row appears. Every run is recorded in `ingestion_run`. The actual pipeline is a pluggable interface (implemented in T-019).

## Context
- `AGENTS.md`, `specs/03-conventions.md`
- `specs/01-architecture.md` §1 (API/worker communicate via the DB), §2.1 (Worker layout), §3.2, §4 (Cronos)
- `specs/02-data-model.md` §2.3

## Scope
You may create or modify **only**:
- `src/GetABursary.Worker/Scheduling/**`
- `src/GetABursary.Worker/Pipeline/IIngestionPipeline.cs`
- `src/GetABursary.Worker/Program.cs`, `src/GetABursary.Worker/appsettings.json`, `src/GetABursary.Worker/GetABursary.Worker.csproj`
- `src/GetABursary.Core/Repositories/Ingestion/IIngestionRunRepository.cs`, `IngestionRunRepository.cs`
- `Directory.Packages.props` (add `Cronos`)
- `tests/GetABursary.Worker.Tests/Scheduling/**`, `tests/GetABursary.Core.Tests/Repositories/Ingestion/**`

## Requirements
1. `IIngestionPipeline.RunAsync(Guid runId, Guid? sourceId, CancellationToken ct)` (interface only). Register a `NoOpIngestionPipeline` until T-019.
2. `IngestionScheduler : BackgroundService`:
   - reads `Ingestion:Cron` (default `0 3 * * *`) and `Ingestion:TimeZone` (default `Africa/Johannesburg`), parsed with Cronos
   - every `Ingestion:RequestPollSeconds` (default 60) checks for unpicked `ingestion_run_request` rows, marks them picked, and triggers a run (with `sourceId` if set)
   - only one run at a time (`SemaphoreSlim`). Requests arriving during a run wait for the next poll
   - each run: create an `IngestionRun` (`Running`), call the pipeline in a new DI scope, then set `Succeeded` or `Failed` + `finished_at`. Exceptions are logged and never crash the host
3. Separate the time-dependent logic for testing: `IRunTrigger`/`ScheduleCalculator.GetNextOccurrence(DateTimeOffset now)` using the injected `IClock`.
4. `IngestionRunRepository`: `CreateRunAsync`, `CompleteRunAsync`, `GetPendingRequestsAsync`, `MarkRequestPickedAsync`, `SaveChangesAsync`.

## Acceptance criteria
- [ ] Unit test: next occurrence of `0 3 * * *` in SAST from a given instant is correct (including the after-03:00 case → next day)
- [ ] Unit test: a pipeline exception results in run status `Failed`, and the scheduler continues
- [ ] Integration test: a pending request is picked up once (not twice) and produces a run with that `sourceId`
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Scheduling|FullyQualifiedName~Repositories.Ingestion"
```
