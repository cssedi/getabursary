---
id: T-015
title: Source adapter for StudyTrust (studytrust)
phase: 2
area: worker
model: sonnet
status: done
depends_on: [T-013]
requirements: [FR-21, FR-22, FR-27]
issue: 15
---

## Goal
Bursary listings from **StudyTrust** (https://studytrust.org.za) can be discovered and parsed into `RawListing`s, tested against saved HTML fixtures.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §5 (adapter tests) and §7 (scraper etiquette)
- `specs/00-product-brief.md` §6.3 (content policy: facts + link only)
- `specs/01-architecture.md` §3.2
- The contract from T-013: `src/GetABursary.Worker/Sources/ISourceAdapter.cs`, `SourceAdapterBase.cs`, `Parsing/SaDateParser.cs`

## Scope
You may create or modify **only**:
- `src/GetABursary.Worker/Sources/Adapters/StudyTrust/**`
- `src/GetABursary.Worker/Program.cs` (one line: register the adapter)
- `data/seed/sources.json` (set `"enabled": true` for `studytrust` only)
- `tests/GetABursary.Worker.Tests/Sources/Adapters/StudyTrust/**`
- `tests/GetABursary.Worker.Tests/Fixtures/studytrust/**`

## Requirements
1. **First check `https://studytrust.org.za/robots.txt`.** If listing or detail pages are disallowed for `*` or for our bot, stop and comment `BLOCKED: robots.txt disallows ...` on the issue. Also read the site's terms page if there is one, and mention anything relevant in the PR.
2. Save fixtures (one-off `curl` with our User-Agent, at most about 10 requests in total, 3 s apart): at least **1 index/listing page** and **3 detail pages** that look different from each other (e.g. one with no closing date). Name them `index-1.html`, `detail-<slug>.html`. Strip `<script>` tags from the fixtures.
3. `StudyTrustAdapter : SourceAdapterBase, ISourceAdapter` with `SourceKey = "studytrust"`:
   - `DiscoverAsync`: walk the site's bursary index (pagination up to `options.MaxPages`), yield detail-page URLs (absolute, de-duplicated, at most `MaxListings`). Only bursary listings, not news, categories or ads
   - `Parse`: extract `Title`, `FunderName` (if stated), `ClosingDate`/`OpenDate` (via `SaDateParser`), `ApplyUrl` (the **external** link to the funder/application, not an internal link, if present), `EligibilityText` (requirements/eligibility/criteria sections, cleaned, ≤ 8000 chars), `FieldsHint` (any "fields of study"/"qualifying degrees" text), `ValueText` (what's covered)
   - return a `Result` failure `Parse.NotABursary` if the page doesn't look like a bursary listing (e.g. no title or eligibility text)
4. Keep selectors in one place (`StudyTrustSelectors` constants) so they're easy to fix when the site changes.
5. Tests use only fixtures and a fake `IPageFetcher`, with no network.

## Acceptance criteria
- [ ] `StudyTrustAdapterTests.Discover_FromIndexFixture_ReturnsDetailUrls` (asserts the count and a known URL)
- [ ] One `Parse_...` test per detail fixture asserting exact `Title`, `ClosingDate` (or null), and that `EligibilityText` contains a known phrase
- [ ] `Parse` on a non-bursary page (e.g. the index fixture) returns `Parse.NotABursary`
- [ ] `sources.json` has `studytrust` enabled; nothing else changed
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Adapters.StudyTrust"
```
