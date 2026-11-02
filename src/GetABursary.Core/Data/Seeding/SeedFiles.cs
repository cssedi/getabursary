namespace GetABursary.Core.Data.Seeding;

// JSON shapes for the seed files. Enum-valued fields stay as strings and are parsed by DataSeeder,
// so a bad value is reported with its file name. Unknown JSON properties (such as "$schema") are ignored.

internal sealed class FieldOfStudyFile
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public string? ParentCode { get; init; }
}

internal sealed class InstitutionFile
{
    public required string ShortName { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public required string Type { get; init; }

    public required string Province { get; init; }

    public required string WebsiteUrl { get; init; }
}

internal sealed class SourceFile
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string BaseUrl { get; init; }

    public string? IconUrl { get; init; }

    public required bool Enabled { get; init; }

    public required int CrawlDelayMs { get; init; }
}

internal sealed class FacultiesFile
{
    public required string InstitutionSlug { get; init; }

    public required List<FacultyFile> Faculties { get; init; }
}

internal sealed class FacultyFile
{
    public required string Name { get; init; }

    public required string Slug { get; init; }

    public required List<QualificationFile> Qualifications { get; init; }
}

internal sealed class QualificationFile
{
    public required string Name { get; init; }

    public string? Abbreviation { get; init; }

    public required string StudyLevel { get; init; }

    public int? NqfLevel { get; init; }

    public int? DurationYears { get; init; }

    public required string Slug { get; init; }

    public required List<string> FieldCodes { get; init; }
}
