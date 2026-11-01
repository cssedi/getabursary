---
id: T-017
title: Eligibility extractor (LLM implementation + deterministic fake)
phase: 2
area: worker
model: sonnet
status: todo
depends_on: [T-009, T-013]
requirements: [FR-02, FR-23]
issue: 17
---

## Goal
Free-text bursary listings are turned into structured eligibility (CESM field codes, study levels, minimum marks, citizenship, coverage, dates and a short summary in our own words), each with a confidence score. This is what makes degree matching possible.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §2, §7
- `specs/00-product-brief.md` §4.3 (FR-23), §6.3 (content policy)
- `specs/01-architecture.md` §2.1 (`Extraction/`), §4, §5 (`Extraction:*` settings)
- `specs/02-data-model.md` §1 (enums), §2.2 (`bursary` columns), §3.1 (CESM codes)

## Scope
You may create or modify **only**:
- `src/GetABursary.Worker/Extraction/**`
- `src/GetABursary.Worker/Program.cs` (registration), `src/GetABursary.Worker/appsettings.json` (non-secret defaults only)
- `src/GetABursary.Core/Repositories/Taxonomy/IFieldOfStudyRepository.cs`, `FieldOfStudyRepository.cs` (read-only: list all)
- `tests/GetABursary.Worker.Tests/Extraction/**`

## Requirements
1. ```csharp
   public interface IEligibilityExtractor { Task<Result<ExtractedEligibility>> ExtractAsync(RawListing listing, CancellationToken ct); }
   public sealed record ExtractedEligibility(
       IReadOnlyList<string> FieldCodes, bool AllFieldsEligible, IReadOnlyList<string> QualificationNames,
       IReadOnlyList<string> InstitutionShortNames, IReadOnlyList<StudyLevel> StudyLevels, decimal? MinAverage,
       IReadOnlyList<SubjectRequirement> SubjectRequirements, Citizenship Citizenship, IReadOnlyList<Province> Provinces,
       IReadOnlyList<CoverageItem> Coverage, bool? WorkBackRequired, string? WorkBackDetails,
       DateOnly? ClosingDate, DateOnly? OpenDate, string? FunderName, string Summary, decimal Confidence);
   ```
2. `LlmEligibilityExtractor` (`Extraction:Provider = "anthropic"`):
   - calls `POST https://api.anthropic.com/v1/messages` with a typed `HttpClient`, headers `x-api-key` (from `Extraction:ApiKey`, **never logged**) and `anthropic-version: 2023-06-01`. Model from `Extraction:Model` (default `claude-haiku-5-5`), `max_tokens` 1500, temperature 0
   - forces structured output with a single tool (`record_eligibility`) whose `input_schema` mirrors `ExtractedEligibility`, using `tool_choice: { type: "tool", name: "record_eligibility" }`
   - the system prompt includes the allowed CESM codes + names (loaded from `IFieldOfStudyRepository`, cached), the enum values, and the rules: *only use facts present in the text; `Summary` ≤ 600 chars in your own words (don't copy sentences); `Confidence` 0–1 reflects how explicit the text was*
   - post-validation: drop unknown field codes (each one dropped lowers confidence by 0.1); clamp `MinAverage` to 0–100; if the listing's parsed `ClosingDate` exists, it wins over the model's
   - HTTP/parse failure → `Result` failure `Extraction.Failed` (the pipeline will queue the listing for review)
3. `FakeEligibilityExtractor` (`Extraction:Provider = "fake"`, the default in Development and in tests): deterministic keyword rules (e.g. "engineering" → `08`, "accounting|commerce|finance" → `04`, "matric|grade 12" → `Matric`, "honours" → `Honours`, "(\d{2})% average" → `MinAverage`), confidence 0.5.
4. Never send personal data to the LLM. Only listing text is sent.

## Acceptance criteria
- [ ] `LlmEligibilityExtractorTests` with a fake `HttpMessageHandler`: request has the right headers/model/tool_choice; a canned tool-use response maps correctly; unknown codes are dropped and confidence reduced; a 500 response → `Extraction.Failed`; the API key never appears in logs (use a test logger)
- [ ] `FakeEligibilityExtractorTests`: ≥ 5 rule cases
- [ ] No API key in any committed file
- [ ] `dotnet build` (zero warnings) and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Extraction"
git grep -n "sk-ant" && echo "KEY LEAK" || echo "clean"
```
