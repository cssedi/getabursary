---
id: T-055
title: Clean scraped funder names (email-protection junk, admin notes in brackets)
phase: 7
area: worker
model: haiku
status: todo
depends_on: []
requirements: [FR-22]
issue: 105
---

## Goal
Funder names read like names ("SRK Consulting"), not like scraped page fragments ("SRK Consulting (South Africa) (administered in-house; [email protected])").

## Context
- `AGENTS.md`, `specs/03-conventions.md`, the worker parse/merge code (`src/GetABursary.Worker/**`)
- Live examples: "SRK Consulting (South Africa) (administered in-house; [email protected])", "Exxaro Resources Limited (applications via the Exxaro eRecruit portal)", "South African Institution of Civil Engineering (SAICE) — administered in-house via the SAICE Patrons' Engineering Bursary Scheme (SPEBS) desk"

## Scope
- `src/GetABursary.Worker/Pipeline/**` or `Merging/**` (one static `FunderName.Clean`, applied before merge), `src/GetABursary.Core/Data/Migrations/**` (**one** migration `CleanFunderNames` that applies the same rules to existing rows with SQL, skipping rows whose `locked_fields` contains `FunderName`), `tests/GetABursary.Worker.Tests/**`

## Requirements
1. Rules, in order: remove Cloudflare email-protection text (`[email protected]`, `[email&#160;protected]`); drop bracketed parts containing "administered", "applications via", "apply via", "portal", "in-house", "@"; drop text after " — " or " - " when it starts with "administered"/"applications"; keep a short bracketed acronym like "(SAICE)" or "(GE)"; collapse spaces; trim punctuation.
2. Never return empty: fall back to the original trimmed name.

## Acceptance criteria
- [ ] Table-driven tests for the live examples above plus 5 normal names that must stay unchanged
- [ ] The migration test (or a test against Testcontainers) shows existing rows cleaned and locked rows untouched
- [ ] `dotnet build` and `dotnet test` pass; Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Funder"
```
