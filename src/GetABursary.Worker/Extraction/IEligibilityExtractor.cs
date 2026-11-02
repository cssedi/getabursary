using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Worker.Sources;

namespace GetABursary.Worker.Extraction;

public interface IEligibilityExtractor
{
    Task<Result<ExtractedEligibility>> ExtractAsync(RawListing listing, CancellationToken ct);
}

public sealed record ExtractedEligibility(
    IReadOnlyList<string> FieldCodes,
    bool AllFieldsEligible,
    IReadOnlyList<string> QualificationNames,
    IReadOnlyList<string> InstitutionShortNames,
    IReadOnlyList<StudyLevel> StudyLevels,
    decimal? MinAverage,
    IReadOnlyList<SubjectRequirement> SubjectRequirements,
    Citizenship Citizenship,
    IReadOnlyList<Province> Provinces,
    IReadOnlyList<CoverageItem> Coverage,
    bool? WorkBackRequired,
    string? WorkBackDetails,
    DateOnly? ClosingDate,
    DateOnly? OpenDate,
    string? FunderName,
    string Summary,
    decimal Confidence);

public static class ExtractionErrors
{
    public static Error Failed(string reason) =>
        new("Extraction.Failed", $"Eligibility extraction failed: {reason}", ErrorType.Unexpected);
}
