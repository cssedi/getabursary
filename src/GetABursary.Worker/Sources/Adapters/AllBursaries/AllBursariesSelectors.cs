namespace GetABursary.Worker.Sources.Adapters.AllBursaries;

/// <summary>Every CSS selector and label keyword the AllBursaries adapter relies on. Fix them here when the site changes.</summary>
internal static class AllBursariesSelectors
{
    /// <summary>Path of the page listing the field-of-study categories, relative to the site's base URL.</summary>
    public const string IndexPath = "bursaries";

    /// <summary>On the index, each category is a card link (<c>/accounting</c>).</summary>
    public const string IndexCategoryLinks = "main a.group[href]";

    /// <summary>On a category page, each bursary is a card link (<c>/accounting/agsa-bursary</c>).</summary>
    public const string CategoryListingLinks = "main a.h-full[href]";

    public const string Title = "main h1";

    /// <summary>The funder's name is the first paragraph after the title.</summary>
    public const string Funder = "main h1 + p";

    /// <summary>The summary box under the title: label (dt) then value (dd).</summary>
    public const string SummaryLabel = "main dl dt";

    /// <summary>The external funder / application link opens in a new tab; footer social links are outside main.</summary>
    public const string ApplyLink = "main a[target=_blank][href^=http]";

    public const string SectionHeading = "main h2, main h3";
    public const string SectionItems = "li";
    public const string SectionContainer = "section";
    public const string FieldItems = "span.font-medium";

    // Matched against the heading / label text, case-insensitively.
    public const string EligibilityHeading = "eligibility";
    public const string ValueHeading = "included";
    public const string FieldsHeading = "fields of study";
    public const string DeadlineLabel = "deadline";
    public const string OpenLabel = "open";
    public const string AmountLabel = "amount";
}
