# Tasks

Each `T-xxx-*.md` file is **one unit of work for one implementer agent = one PR**. Files are the source of truth and are mirrored to GitHub Issues by `node scripts/sync-issues.mjs`. The workflow is described in [specs/04-agent-workflow.md](../specs/04-agent-workflow.md). New tasks start from [`_TEMPLATE.md`](_TEMPLATE.md).

## Commands
```bash
node scripts/sync-issues.mjs --check   # validate all task files (CI runs this)
node scripts/sync-issues.mjs --ready   # which tasks can start now?
node scripts/sync-issues.mjs           # create/update GitHub Issues + labels (needs gh auth)
```
To start a task, add the **`agent:ready`** label to its issue. Do this only when every `depends_on` task is merged.

## Phase plan

| Phase | Theme | Status |
|---|---|---|
| **0** | Foundation: solution, CI, dev DB, `Result<T>`, DbContext | Detailed (T-001–T-004) |
| **1** | Data model & reference data | Detailed (T-005–T-010) |
| **2** | Ingestion worker: HTTP, adapters, extraction, dedupe | Detailed (T-011–T-019) |
| **3** | Public read API: taxonomy, search, detail, reports | Detailed (T-020–T-023) |
| **4** | Angular web app: SSR scaffold, API client, degree picker + filters, results, detail, report dialog, SEO pages | Detailed (T-025–T-031) |
| 5 | Accounts (Identity + Google), sign-in prompt, saved bursaries + tracker, admin review queue, source and taxonomy management | Roadmap; detailed when Phase 3 lands |
| 6 | Launch: VPS deploy (Docker Compose + Caddy), ads + consent, privacy policy, monitoring, budget alerts | Roadmap |

## Dependency graph (Phases 0–4)

```mermaid
graph LR
  T001[T-001 solution] --> T003[T-003 Result]
  T001 --> T004[T-004 DbContext]
  T002[T-002 dev DB] --> T004
  T003 --> T004
  T004 --> T005[T-005 taxonomy]
  T005 --> T006[T-006 bursary]
  T006 --> T007[T-007 ingestion/report]
  T008[T-008 seed JSON] --> T009[T-009 seeder]
  T006 --> T009
  T008 --> T010[T-010 faculties UCT+Wits]
  T010 --> T024[T-024 faculties UJ+UP from PDFs]
  T007 --> T011[T-011 worker host]
  T001 --> T012[T-012 polite http]
  T003 --> T012
  T007 --> T013[T-013 adapter contract]
  T012 --> T013
  T013 --> T014[T-014 ZA Bursaries]
  T013 --> T015[T-015 StudyTrust]
  T013 --> T016[T-016 AllBursaries]
  T013 --> T017[T-017 extractor]
  T009 --> T017
  T017 --> T018[T-018 merger]
  T011 --> T019[T-019 pipeline]
  T018 --> T019
  T003 --> T020[T-020 taxonomy API]
  T009 --> T020
  T003 --> T021[T-021 search]
  T020 --> T021
  T006 --> T021
  T021 --> T022[T-022 detail]
  T003 --> T023[T-023 reports]
  T007 --> T023
  T025[T-025 Angular scaffold] --> T026[T-026 API client]
  T026 --> T027[T-027 picker + filters]
  T027 --> T028[T-028 results + cards]
  T026 --> T029[T-029 detail page]
  T028 --> T030[T-030 report dialog]
  T029 --> T030
  T028 --> T031[T-031 SEO pages + sitemap]
  T029 --> T031
```

**Can start immediately (in parallel):** T-001, T-002, T-008.

> Parallel adapter tasks (T-014–T-016) each add one line to `Worker/Program.cs` and `sources.json`. Expect a trivial rebase when merging the second and third.
