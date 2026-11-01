---
id: T-003
title: Add Result<T>, Error, IClock and Result-to-HTTP mapping
phase: 0
area: core
model: haiku
status: todo
depends_on: [T-001]
requirements: [NFR-06]
issue: 3
---

## Goal
The shared building blocks every service uses: `Result<T>` and `Error` for expected failures, `IClock` for testable time, and a single extension that converts a `Result<T>` into the right HTTP response.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §2 (Result and errors) and §3

## Scope
You may create or modify **only**:
- `src/GetABursary.Core/Common/**`
- `src/GetABursary.Api/Extensions/ResultExtensions.cs`
- `src/GetABursary.Api/Extensions/ServiceCollectionExtensions.cs`
- `src/GetABursary.Api/Program.cs` (only to call `AddCommon()` and add `AddProblemDetails()` + `UseExceptionHandler()`)
- `tests/GetABursary.Core.Tests/Common/**`, `tests/GetABursary.Api.Tests/Extensions/**`

## Requirements
1. `Core/Common/ErrorType.cs`: `Validation, NotFound, Conflict, Gone, Unauthorized, Forbidden, Unexpected`.
2. `Core/Common/Error.cs`: `public sealed record Error(string Code, string Message, ErrorType Type)` with static helpers `Validation(code, msg)`, `NotFound(code, msg)`, `Conflict(...)`, `Gone(...)`.
3. `Core/Common/Result.cs`: `Result<T>` with `IsSuccess`, `IsFailure`, `Value`, `Errors` (`IReadOnlyList<Error>`), `Success(T)`, `Failure(params Error[])` (throws `ArgumentException` if no errors are given), and implicit conversions from `T` and from `Error`. Accessing `Value` on a failure throws `InvalidOperationException`.
4. `Core/Common/IClock.cs` + `SystemClock` (`DateTimeOffset UtcNow`, `DateOnly Today` in Africa/Johannesburg time).
5. `Api/Extensions/ResultExtensions.cs`: `IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)` and an overload with a success-status selector (e.g. `201 Created`). Status mapping as in conventions §2. Failures become `ProblemDetails` (title = first error's message, `extensions["errors"]` = list of `{ code, message }`).
6. `Api/Extensions/ServiceCollectionExtensions.cs`: `AddCommon()` registers `IClock` → `SystemClock` (singleton).

## Acceptance criteria
- [ ] Unit tests: `ResultTests` (success, failure, implicit conversions, `Value` on failure throws, empty failure throws)
- [ ] Unit tests: `ResultExtensionsTests`: every `ErrorType` maps to the right status code, and the ProblemDetails contains the error codes
- [ ] `SystemClockTests`: `Today` uses the SAST date
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Common|FullyQualifiedName~Extensions"
```
