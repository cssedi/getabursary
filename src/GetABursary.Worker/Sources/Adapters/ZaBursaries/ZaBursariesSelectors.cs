namespace GetABursary.Worker.Sources.Adapters.ZaBursaries;

/// <summary>Every selector and heading keyword the adapter relies on, so a site change is a one-file fix.</summary>
internal static class ZaBursariesSelectors
{
    /// <summary>Links on index pages; listing URLs are filtered further by <see cref="ListingPathSuffix"/>.</summary>
    public const string IndexLinks = "a[href]";

    /// <summary>Next-page link on paginated WordPress archives.</summary>
    public const string NextPage = "a.next[href], link[rel='next'][href]";

    /// <summary>Listings live at <c>/{category}-bursaries-south-africa/{slug}/</c>; news and category pages don't match.</summary>
    public const string ListingPathSuffix = "-bursaries-south-africa";

    public const string Content = ".entry-content";
    public const string Title = ".entry-content h1";
    public const string Headings = "h2, h3";
    public const string Links = "a[href]";

    // Lower-case keywords found in section headings.
    public static readonly string[] EligibilityHeading = ["eligib", "requirement", "criteria"];
    public static readonly string[] FieldsHeading = ["fields", "qualifying", "degrees"];
    public static readonly string[] ValueHeading = ["expenses", "coverage", "benefit"];
    public static readonly string[] ApplyHeading = ["apply"];
    public static readonly string[] ClosingHeading = ["closing date"];
    public static readonly string[] OpeningHeading = ["opening date", "opens"];

    /// <summary>Provider heading, e.g. "ABOUT THE BURSARY PROVIDER – FASSET".</summary>
    public const string ProviderHeadingPrefix = "about the bursary provider";
}
