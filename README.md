# GetABursary

**Pick your degree, see every bursary you actually qualify for, and track your applications in one place.**

GetABursary is a free platform for South African students: matric learners, undergraduates and postgraduates. It gathers open bursaries from many aggregator sites into one deduplicated index and matches them to a student's **specific university qualification**, study level and marks. Every listing links back to where it was found and to the funder's own site.

> 🚧 Early development. This project is also an experiment in **spec-driven, agentic engineering**: an orchestrator model writes specs and tasks, and smaller models implement them via GitHub Issues.

## How this repo works

| Folder | What's in it |
|---|---|
| [`specs/`](specs/) | Product brief, architecture, data model, conventions, agent workflow, ADRs. **The source of truth.** |
| [`tasks/`](tasks/) | One markdown file per unit of work, mirrored to GitHub Issues |
| `src/` | `GetABursary.Api` (ASP.NET Core), `GetABursary.Worker` (ingestion), `GetABursary.Core` (domain + data), `web/` (Angular) |
| `tests/` | xUnit tests, including Testcontainers integration tests |
| `data/seed/` | Reference data: fields of study (CESM), institutions, sources |

Start with [`specs/README.md`](specs/README.md) and [`specs/04-agent-workflow.md`](specs/04-agent-workflow.md).

## Tech stack
.NET 10 · ASP.NET Core · EF Core + PostgreSQL 17 · Angular (SSR) · GitHub Actions · Claude Code GitHub Action

## Content policy
We store **facts** about bursaries (name, funder, eligibility, dates, links) and always link to the original source and funder. We respect `robots.txt` and crawl politely. If you run a source site and want changes, please open an issue.

## Licence
[MIT](LICENSE)
