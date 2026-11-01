# ADR-002: Orchestrator + GitHub-Issue implementer agents

- **Status:** Accepted, 2026-11-01
- **Deciders:** @cssedi, orchestrator

## Context
The project is also an experiment in agentic engineering. We want a larger "orchestrator" model to own specs and planning, while cheaper, smaller models implement small tasks.

## Decision
- Specs live in `/specs`. Tasks are markdown files in `/tasks` (the source of truth) and are **mirrored to GitHub Issues** by `scripts/sync-issues.mjs`.
- Implementers are the **Claude GitHub Action** (`anthropics/claude-code-action`), triggered by the `agent:ready` label. The model is chosen by a `model:*` label.
- Each PR is reviewed by a separate reviewer run and by CI. The human product owner merges.

## Consequences
- ✅ Every unit of work is traceable: requirement → task file → issue → PR → commit.
- ✅ Small models get narrow, testable tasks, and CI decides when a task is done.
- ✅ Task files are diffable and reviewable, and they survive if we ever switch tool.
- ❌ Task status must be kept in sync between files and Issues (handled by the script and the PR rule "mark task done").
- ❌ API usage costs money per run (mitigated by label gating, `--max-turns`, and a spend limit).
