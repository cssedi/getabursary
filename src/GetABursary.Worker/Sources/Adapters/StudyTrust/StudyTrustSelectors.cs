namespace GetABursary.Worker.Sources.Adapters.StudyTrust;

/// <summary>Every CSS selector and label keyword the StudyTrust adapter relies on. Fix them here when the site changes.</summary>
internal static class StudyTrustSelectors
{
    /// <summary>Path of the bursary index, relative to the site's base URL.</summary>
    public const string IndexPath = "bursaries/";

    /// <summary>Each bursary tile on the index is a logo image wrapped in a link to the detail page.</summary>
    public const string IndexListingLinks = ".et_pb_image a[href]";

    public const string Title = ".et_pb_text_inner h1";

    /// <summary>A detail page is a stack of rows: a blurb heading on the left, the text on the right.</summary>
    public const string SectionRow = ".et_pb_row";
    public const string SectionHeading = ".et_pb_blurb h4";
    public const string SectionBody = ".et_pb_text_inner";

    public const string ApplyButton = "a.et_pb_button[href]";

    // Section headings are matched by keyword (lower case), first match wins in this order.
    public static readonly string[] FieldsHeadings = ["field", "qualifying", "degrees"];
    public static readonly string[] EligibilityHeadings = ["eligib", "criteria", "who can apply", "requirement", "when applying"];
    public static readonly string[] ValueHeadings = ["funding", "value", "benefit", "covers"];
    public static readonly string[] DateHeadings = ["closing", "deadline", "how to apply"];
}
