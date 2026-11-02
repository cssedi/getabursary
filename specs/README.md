# Specs

Specs are the source of truth. Code follows the specs. If the code needs to differ, **change the spec first** (in the same PR or a preceding one).

| # | Spec | Status |
|---|---|---|
| 00 | [Product brief & requirements](00-product-brief.md) | Accepted v0.4 |
| 01 | [Architecture](01-architecture.md) | Accepted v1.0 |
| 02 | [Data model & matching](02-data-model.md) | Accepted v1.0 |
| 03 | [Engineering conventions](03-conventions.md) | Accepted v1.0 |
| 04 | [Agentic development workflow](04-agent-workflow.md) | Accepted v1.1 |
| 05 | [Web app (Angular)](05-web.md) | Accepted v1.1 |
| 06 | [Design system & interface rules](06-design.md) | Accepted v1.0 (governs frontend) |

## Architecture Decision Records
| ADR | Title | Status |
|---|---|---|
| [ADR-001](adr/ADR-001-postgres-only.md) | PostgreSQL only (no graph DB for MVP) | Accepted |
| [ADR-002](adr/ADR-002-agent-workflow.md) | Orchestrator + GitHub-Issue implementer agents | Accepted |

## Requirement IDs
`FR-xx` (functional) and `NFR-xx` (non-functional) are defined in `00-product-brief.md`. Task files list the IDs they deliver under `requirements:`, which gives traceability from requirement → task → issue → PR.

## Decision log
| Date | Decision | Where |
|---|---|---|
| 2026-11-01 | PostgreSQL only | ADR-001 |
| 2026-11-01 | Orchestrator + GitHub-Issue implementers | ADR-002 |
| 2026-11-02 | Marks filter = overall average only | Brief FR-01, Q5 |
| 2026-11-02 | MVP qualifications for the big 5 (UCT, Wits, UP, SU, UJ) | Brief FR-32 |
| 2026-11-02 | Account required only for actions that save the user's own data | Brief FR-09, Architecture §3.3 |
| 2026-11-02 | Web: Tailwind + Angular CDK, no component library; same-origin hosting (no CORS) | Spec 05 §1–§2 |
| 2026-11-02 | Auto-merge green + approved PRs; human only for flagged cases | Spec 04 |
| 2026-11-02 | Frontend governed by frontend-design + Web Interface Guidelines: jacaranda/highlighter palette, Atkinson Hyperlegible Next, deadline-ledger layout; sentence case | Spec 06 |
