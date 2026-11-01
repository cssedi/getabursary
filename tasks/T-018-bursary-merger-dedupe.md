---
id: T-018
title: Bursary merger with deduplication rules and locked-field protection
phase: 2
area: worker
model: sonnet
status: todo
depends_on: [T-017]
requirements: [FR-05, FR-24, FR-41, FR-42]
issue: 18
---

## Goal
Each parsed + extracted listing is merged into exactly one canonical bursary: matched to an existing one when it's the same bursary, created otherwise, or flagged for review when unsure. Admin-locked fields are never overwritten, and source links accumulate (powering the multi-source icons).

## Context
- `AGENTS.md`, `specs/03-conventions.md`
- `specs/02-data-model.md` §2.2, §2.4 (`review_item`), **§4 (deduplication rules: implement exactly)**
- `specs/01-architecture.md` §3.2

## Scope
You may create or modify **only**:
- `src/GetABursary.Worker/Pipeline/Merging/**`
- `src/GetABursary.Core/Repositories/Bursaries/IBursaryWriteRepository.cs`, `BursaryWriteRepository.cs`
- `src/GetABursary.Core/Repositories/Bursaries/IFunderRepository.cs`, `FunderRepository.cs`
- `src/GetABursary.Core/Repositories/Reports/IReviewItemRepository.cs`, `ReviewItemRepository.cs`
- `src/GetABursary.Core/Domain/Bursaries/NameNormalizer.cs`, `SlugGenerator.cs`
- `src/GetABursary.Worker/Program.cs` (registration)
- `tests/GetABursary.Worker.Tests/Pipeline/Merging/**`, `tests/GetABursary.Core.Tests/Domain/Bursaries/**`

## Requirements
1. `NameNormalizer.Normalize(string)` exactly as in spec §4.1. Use it for both funder and bursary names.
2. `SlugGenerator.Create(name, id)` → `kebab-case-name-<first 6 hex chars of id>`.
3. `BursaryMerger.MergeAsync(Guid sourceId, Uri sourceUrl, Guid rawListingId, RawListing raw, ExtractedEligibility ex, ct) → MergeOutcome { Created | Merged | PossibleDuplicate, BursaryId }`:
   - get or create the `Funder` by normalised name (if any)
   - find candidates with `EF.Functions.TrigramsSimilarity` ≥ 0.4, and apply the **auto-merge / possible-duplicate / new** rules from spec §4.3–4.5
   - **merge:** update every bursary field from `ex`/`raw` **except** those in `LockedFields`. Replace field/qualification/institution links only if `"Eligibility"` isn't locked. Never downgrade `extraction_confidence` with a lower value on unchanged text
   - map `QualificationNames` → `bursary_qualification` by case-insensitive name match within `InstitutionShortNames` (ignore names with no match), and `InstitutionShortNames` → `bursary_institution`
   - upsert `bursary_source` (`first_seen_at` kept, `last_seen_at` = now). Set `raw_listing.bursary_id`
   - new bursaries: `is_published = false` + `ReviewItem(NewListing)`. Merged with confidence < 0.7 → `ReviewItem(LowConfidence)` (unless one is already open for that bursary)
   - status: closing date < today → `Closed`, null → `Unknown`, else `Open`
4. One `SaveChangesAsync` per merge.

## Acceptance criteria
- [ ] `NameNormalizerTests` (years, stop words, punctuation, case) and `SlugGeneratorTests`
- [ ] Integration tests (Postgres fixture), one per rule: new bursary created unpublished + review item; same funder & similar name & close dates → merged with 2 `bursary_source` rows; similarity 0.4–0.6 → possible duplicate, not merged; locked `ClosingDate` not overwritten; low confidence → one review item, not duplicated on re-merge
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Merging|FullyQualifiedName~Domain.Bursaries"
```
