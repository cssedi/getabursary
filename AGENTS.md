# Agent Instructions — GetABursary

## Session canary
- Begin **every** response, issue comment and PR description with `Lesdee,`. This is a deliberate context-integrity canary: if it's missing, the reader assumes you didn't load this file.

## What this project is
GetABursary helps South African students find bursaries they actually qualify for, matched by university qualification, study level and marks, with an application tracker. Read `specs/00-product-brief.md` for the why and `specs/01-architecture.md` for the how.

## Before you write code
1. Read your task file in `tasks/` (it's linked from the issue). Read **every** spec section listed under "Context".
2. Read `specs/03-conventions.md`. It's mandatory, and PRs that break it will be rejected.
3. Only create or modify files listed in the task's **Scope**. If you need anything else, stop and escalate (below).

## How to implement: smallest complete change ("ponytail")
Product-owner rule (2026-11-02). Be lazy about the solution, never about finishing it.
1. **Before writing:** read the task and the code it touches. List every place the change must reach (callers, tests, fixtures, config, DI registration). That is the scope. Extra features are not.
2. **Take the first option that fully works:** does it need to exist at all → already in this repo (helper, service, component, pattern; reuse it the way surrounding code does) → .NET/Angular/standard library or platform → an installed dependency → a one-liner a reader gets at a glance → otherwise the minimum code that works.
3. **No** abstractions, wrappers, options, config, interfaces with one implementation beyond what the layering rules require, or "for later" code. Keep the existing structure (Controller → Service → Repository, folders, conventions). Deletion beats addition.
4. **Bug fixes:** grep every caller first, then fix the root cause once in the shared code.
5. **Tests:** new non-trivial logic (a branch, loop, parser, money, security, data writes, a bug fix) gets at least one small test that fails when it breaks. Trivial changes need none beyond the task's acceptance criteria.
6. **Known limits:** mark a deliberate shortcut with a one-line comment `// shortcut: <the limit>, <when to upgrade>`.
7. **Never cut:** input validation at trust boundaries, error handling that prevents data loss, security, accessibility, anything the task asks for.
8. **PR description:** end with one or two lines: what you skipped or didn't check, and any risk.

## Frontend work (anything under `src/web/`)
- `specs/06-design.md` **governs** all visual design, copy and interface behaviour. Read §2–§8 before writing UI. If a task file or `05-web.md` disagrees with it, 06 wins.
- Before opening the PR, go through the **§7 checklist** for every file you touched, and add a "Design checklist" section to the PR listing anything you couldn't meet and why.
- Never introduce: card grids with shadows, gradients, all-caps labels, middle-dot meta strings, `→` in button text, `transition: all`, hard-coded date formats, or colours outside the §3 tokens.

## Standing scope allowance (applies to every task)
On top of your task's Scope, you **may** change these when your work legitimately needs it, without escalating:
- **Test infrastructure:** test project `.csproj` files (adding packages already pinned in `Directory.Packages.props`), shared fixtures/factories (e.g. Postgres/Testcontainers fixtures, `WebApplicationFactory` subclasses), and test helpers under `tests/**` or `src/web/src/testing/**`.
- **Existing tests broken by your intended change:** update assertions in existing spec/test files (e.g. `src/web/src/app/app.spec.ts`, or route tests that hit a page you're replacing) so they test the new, correct behaviour. Never delete a test just to make it pass, and never weaken what it checks.
- List every such file under a **"Scope allowance used"** heading in the PR description, with one line each on why.
Anything else outside Scope still means: stop and escalate.

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
