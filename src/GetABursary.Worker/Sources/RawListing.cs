namespace GetABursary.Worker.Sources;

/// <summary>The fields an adapter pulled out of one listing page, before any AI extraction.</summary>
public sealed record RawListing(
    Uri SourceUrl,
    string Title,
    string? FunderName,
    DateOnly? ClosingDate,
    DateOnly? OpenDate,
    Uri? ApplyUrl,
    string EligibilityText,
    string? FieldsHint,
    string? ValueText);
