# 04 — Agentic Development Workflow

> Status: **Accepted v1.0**. Decision record: [ADR-002](adr/ADR-002-agent-workflow.md).

## 1. Roles

| Role | Who | Responsibilities |
|---|---|---|
| **Product owner** | @cssedi | Owns `specs/00-product-brief.md`; approves specs; decides open questions; **merges PRs**; adds the `agent:ready` label |
| **Orchestrator** | Claude (larger model), run in a session with the product owner | Turns specs into tasks; writes `tasks/T-xxx-*.md`; syncs them to Issues; reviews PRs against the specs; updates specs when reality changes |
| **Implementer** | Claude GitHub Action (smaller model chosen per task: `model:haiku` or `model:sonnet`) | Implements exactly **one** task, inside its file scope, with tests; opens a PR |
| **Reviewer** | Claude GitHub Action on PRs (separate run, Sonnet) + CI | Checks the PR against the task's acceptance criteria and `specs/03-conventions.md`; comments; never merges |

## 2. Lifecycle of a task

```
 spec change ─▶ orchestrator writes tasks/T-0xx-*.md  (status: todo)
             ─▶ scripts/sync-issues.mjs  ─▶ GitHub Issue (#N) with labels: task, phase:N, area:x, model:x
             ─▶ AUTOPILOT (.github/workflows/autopilot.yml, on every push to main):
                  picks ready tasks, critical path first, up to AUTOPILOT_MAX_PARALLEL (default 2) open at once,
                  labels them agent:in-progress and runs implement.yml for each
                (manual alternative: product owner adds label agent:ready → claude.yml)
             ─▶ implementer: branch task/T-0xx, code + tests, marks the task done, opens PR "Closes #N"
             ─▶ CI (ci.yml) + AI reviewer (claude-review.yml) ─▶ reviewer requests the product owner's review (notification)
             ─▶ fixes via "@claude …" comments on the PR (owner only)
             ─▶ product owner merges  ─▶ issue closes  ─▶ push to main ─▶ autopilot picks the next tasks
```

**Autopilot rules**
- Switch: repository variable `AUTOPILOT` = `on` | `off`. Optional `AUTOPILOT_MAX_PARALLEL` (default `2`).
- Priority: longest chain of tasks waiting on it ("critical path"), then how many tasks it unlocks, then task ID (`node scripts/sync-issues.mjs --ready`).
- A task with an open PR or the `agent:in-progress`/`agent:blocked` label is never started twice. Open PRs awaiting review count against the parallel limit, so unreviewed work can't pile up.
- If a run fails or ends without a PR, the issue gets `agent:blocked` with a link to the run. Remove the label to let the autopilot retry.
- If a PR is closed **without** merging, remove `agent:in-progress` from its issue so the task can be picked again.

**Status values** (task front-matter): `todo` → `ready` (dependencies done) → `in-progress` → `done` | `blocked`.

## 3. What makes a good task (orchestrator checklist)
- **Small:** one PR, ideally under about 300 changed lines (excluding generated migrations and fixtures).
- **Self-contained:** the "Context" section lists the exact spec sections to read. The implementer should never need to guess.
- **Explicit scope:** a list of files/folders the implementer may create or modify.
- **Checkable:** acceptance criteria are concrete checkboxes, and "How to verify" lists commands (`dotnet test --filter …`).
- **Right-sized model:** `model:haiku` for mechanical work (entities from a table, CRUD endpoints, seed JSON in a given format). `model:sonnet` for design-heavy work (pipeline, matching, dedupe, scaffolding).
- **Safe to run in parallel:** tasks with no dependency between them don't touch the same files. Shared registration points (e.g. `Program.cs`) are touched through per-feature extension methods to avoid merge conflicts.

## 4. Escalation {#escalation}
If an implementer:
- finds the spec ambiguous or contradictory,
- needs to change a file outside its scope, or
- can't make the tests pass within the task's approach,

it must **stop**, push what it has (if useful) to the branch, and comment on the issue with:
`BLOCKED: <one-line reason>` followed by the specific question and the options it sees.
The product owner then adds the `agent:blocked` label and brings it to the orchestrator. Guessing is worse than asking.

## 5. Context-integrity canary
`AGENTS.md` contains a canary instruction: every issue comment and PR description from an agent starts with `Lesdee,`. If an agent's output doesn't, it probably didn't load `AGENTS.md`. Treat that output with suspicion and check it against the conventions.

## 6. Cost & safety controls
- The implementer runs **only** when the `agent:ready` label is added. In a public repo, only collaborators with triage access or higher can add labels, so strangers can't spend API credit.
- `@claude` comment triggers are limited by the action to users with write access.
- Each run is capped with `--max-turns`. Models are chosen per task by label.
- The `ANTHROPIC_API_KEY` secret is stored in **repo secrets** and never in code. Set a monthly spend limit in the Anthropic Console.

## 7. Phases (progressive elaboration)
Only the next one or two phases are broken into detailed task files. Later phases stay as a roadmap in `tasks/README.md` until the earlier phases have taught us enough to specify them well.
