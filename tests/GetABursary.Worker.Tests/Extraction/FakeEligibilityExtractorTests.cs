using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Worker.Extraction;
using GetABursary.Worker.Sources;

namespace GetABursary.Worker.Tests.Extraction;

public class FakeEligibilityExtractorTests
{
    private static RawListing Listing(string text, string title = "Bursary", DateOnly? closing = null) =>
        new(new Uri("https://example.org/b"), title, "Funder", closing, null, null, text, null, null);

    private static async Task<ExtractedEligibility> RunAsync(RawListing l)
    {
        var result = await new FakeEligibilityExtractor().ExtractAsync(l, CancellationToken.None);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public async Task ExtractAsync_Engineering_MapsToCode08()
    {
        var r = await RunAsync(Listing("For students of Engineering."));
        Assert.Equal(["08"], r.FieldCodes);
    }

    [Theory]
    [InlineData("BCom Accounting")]
    [InlineData("Commerce degrees")]
    [InlineData("Finance studies")]
    public async Task ExtractAsync_BusinessKeywords_MapToCode04(string text)
    {
        var r = await RunAsync(Listing(text));
        Assert.Equal(["04"], r.FieldCodes);
    }

    [Theory]
    [InlineData("Open to matric learners")]
    [InlineData("Grade 12 learners")]
    public async Task ExtractAsync_MatricKeywords_MapToMatricLevel(string text)
    {
        var r = await RunAsync(Listing(text));
        Assert.Equal([StudyLevel.Matric], r.StudyLevels);
    }

    [Fact]
    public async Task ExtractAsync_Honours_MapsToHonoursLevel()
    {
        var r = await RunAsync(Listing("Honours students only"));
        Assert.Equal([StudyLevel.Honours], r.StudyLevels);
    }

    [Fact]
    public async Task ExtractAsync_AveragePercent_SetsMinAverage()
    {
        var r = await RunAsync(Listing("Requires a 65% average."));
        Assert.Equal(65m, r.MinAverage);
    }

    [Fact]
    public async Task ExtractAsync_NoKeywords_ReturnsEmptyWithHalfConfidence()
    {
        var r = await RunAsync(Listing("Nothing relevant"));
        Assert.Empty(r.FieldCodes);
        Assert.Empty(r.StudyLevels);
        Assert.Null(r.MinAverage);
        Assert.Equal(0.5m, r.Confidence);
    }

    [Fact]
    public async Task ExtractAsync_ClosingDate_IsCarriedThrough()
    {
        var date = new DateOnly(2027, 1, 31);
        var r = await RunAsync(Listing("x", closing: date));
        Assert.Equal(date, r.ClosingDate);
    }
}
