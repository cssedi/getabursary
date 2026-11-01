# Specs

Specs are the source of truth. Code follows the specs. If the code needs to differ, **change the spec first** (in the same PR or a preceding one).

| # | Spec | Status |
|---|---|---|
| 00 | [Product brief & requirements](00-product-brief.md) | Accepted v0.3 |
| 01 | [Architecture](01-architecture.md) | Accepted v1.0 |
| 02 | [Data model & matching](02-data-model.md) | Accepted v1.0 |
| 03 | [Engineering conventions](03-conventions.md) | Accepted v1.0 |
| 04 | [Agentic development workflow](04-agent-workflow.md) | Accepted v1.0 |

## Architecture Decision Records
| ADR | Title | Status |
|---|---|---|
| [ADR-001](adr/ADR-001-postgres-only.md) | PostgreSQL only (no graph DB for MVP) | Accepted |
| [ADR-002](adr/ADR-002-agent-workflow.md) | Orchestrator + GitHub-Issue implementer agents | Accepted |

## Requirement IDs
`FR-xx` (functional) and `NFR-xx` (non-functional) are defined in `00-product-brief.md`. Task files list the IDs they deliver under `requirements:`, which gives traceability from requirement → task → issue → PR.

## Still open (from the brief)
- Q5: minimum marks, overall average only vs subject-specific marks (MVP: average now, subject requirements stored for later)
- Q6: institutions beyond the 26 public universities
- Q7: remembering logged-out users' filters
- Anonymous → account merge behaviour for the tracker
