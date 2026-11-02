using System.Globalization;
using System.Text.RegularExpressions;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Worker.Sources;

namespace GetABursary.Worker.Extraction;

/// <summary>Deterministic keyword-rule extractor for development and tests.</summary>
public sealed partial class FakeEligibilityExtractor : IEligibilityExtractor
{
    public Task<Result<ExtractedEligibility>> ExtractAsync(RawListing listing, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(listing);
        var text = $"{listing.Title} {listing.FieldsHint} {listing.EligibilityText}";

        var codes = new List<string>();
        if (EngineeringRegex().IsMatch(text)) { codes.Add("08"); }
        if (BusinessRegex().IsMatch(text)) { codes.Add("04"); }

        var levels = new List<StudyLevel>();
        if (MatricRegex().IsMatch(text)) { levels.Add(StudyLevel.Matric); }
        if (HonoursRegex().IsMatch(text)) { levels.Add(StudyLevel.Honours); }

        decimal? average = null;
        var m = AverageRegex().Match(text);
        if (m.Success)
        {
            average = decimal.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        var summary = $"Bursary listing: {listing.Title}";
        if (summary.Length > 600)
        {
            summary = summary[..600];
        }

        var result = new ExtractedEligibility(
            codes, false, [], [], levels, average, [], Citizenship.Unknown, [], [],
            null, null, listing.ClosingDate, listing.OpenDate, listing.FunderName, summary, 0.5m);
        return Task.FromResult(Result<ExtractedEligibility>.Success(result));
    }

    [GeneratedRegex("engineering", RegexOptions.IgnoreCase)]
    private static partial Regex EngineeringRegex();

    [GeneratedRegex("accounting|commerce|finance", RegexOptions.IgnoreCase)]
    private static partial Regex BusinessRegex();

    [GeneratedRegex(@"matric|grade\s*12", RegexOptions.IgnoreCase)]
    private static partial Regex MatricRegex();

    [GeneratedRegex("honours", RegexOptions.IgnoreCase)]
    private static partial Regex HonoursRegex();

    [GeneratedRegex(@"(\d{2})\s*%\s*average", RegexOptions.IgnoreCase)]
    private static partial Regex AverageRegex();
}
