# Agent Instructions — GetABursary

## Session canary
- Begin **every** response, issue comment and PR description with `Lesdee,`. This is a deliberate context-integrity canary: if it's missing, the reader assumes you didn't load this file.

## What this project is
GetABursary helps South African students find bursaries they actually qualify for, matched by university qualification, study level and marks, with an application tracker. Read `specs/00-product-brief.md` for the why and `specs/01-architecture.md` for the how.

## Before you write code
1. Read your task file in `tasks/` (it's linked from the issue). Read **every** spec section listed under "Context".
2. Read `specs/03-conventions.md`. It's mandatory, and PRs that break it will be rejected.
3. Only create or modify files listed in the task's **Scope**. If you need anything else, stop and escalate (below).

## Non-negotiable rules
- **Layering:** Controller → Service → Repository. Controllers are thin and services return `Result<T>`. Repositories never take request DTOs and contain no business rules.
- **Tests are required.** Add the tests your task's acceptance criteria name. `dotnet build` (warnings are errors) and `dotnet test` must pass. Adapter tests use saved HTML fixtures, never the live network.
- **No secrets.** This repo is public. Never commit keys, real connection strings or personal data. Use `.env.example` placeholders.
- Use `Guid.CreateVersion7()` for IDs and the injected `IClock` for time. Every I/O method is `async`, takes a `CancellationToken`, and ends in `Async`.
- Changing the database schema means exactly one new EF migration per task. Never edit merged migrations.
- No new NuGet/npm dependency unless the task names it or you justify it in the PR description.

## Finishing a task
1. Branch: `task/T-0xx-short-name`. Commits follow Conventional Commits and mention the task ID.
2. Set `status: done` in the task file's front-matter.
3. Open **one** PR with: `Closes #<issue>`, the acceptance-criteria checklist (ticked), and the verification commands you ran with their results.

## Escalation
If the spec is unclear or contradictory, you need files outside your scope, or the approach doesn't work: **stop**. Comment on the issue with `BLOCKED: <reason>`, your specific question, and the options you see. Don't guess.

## Useful commands
- Build: `dotnet build` (repo root; solution file is `GetABursary.slnx`)
- Test: `dotnet test`
- Local DB: `docker compose up -d db`
- Migration: `dotnet ef migrations add <Name> -p src/GetABursary.Core -s src/GetABursary.Api`
- Web (once it exists): `cd src/web && npm ci && npm test && npm run build`
