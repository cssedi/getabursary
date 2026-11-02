using System.Text.Json;
using GetABursary.Api.DTOs.Admin;
using GetABursary.Api.DTOs.Common;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Reports;
using GetABursary.Core.Repositories.Admin;
using GetABursary.Core.Repositories.Bursaries;

namespace GetABursary.Api.Services.Admin;

public sealed class AdminService(
    IAdminRepository admin,
    IBursaryWriteRepository bursaryWrites,
    IFunderRepository funders,
    IClock clock) : IAdminService
{
    public const int PageSize = 20;

    /// <summary>Same lock name the ingestion merger uses for field-of-study, qualification and institution links.</summary>
    private const string EligibilityLock = "Eligibility";

    private const int MaxNotes = 2000;

    public async Task<Result<PagedResponse<ReviewItemResponse>>> ListReviewItemsAsync(
        ReviewItemType? type, int page, CancellationToken ct)
    {
        if (page < 1)
        {
            return AdminErrors.InvalidPage();
        }

        var (items, total) = await admin.ListOpenReviewItemsAsync(type, (page - 1) * PageSize, PageSize, ct);
        return new PagedResponse<ReviewItemResponse>
        {
            Items = [.. items.Select(ToResponse)],
            Page = page,
            PageSize = PageSize,
            TotalCount = total,
        };
    }

    public async Task<Result<ReviewItemResponse>> ResolveReviewItemAsync(
        Guid id, Guid userId, ResolveReviewItemRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(request.Action))
        {
            return AdminErrors.InvalidAction();
        }

        if (request.Notes is { Length: > MaxNotes })
        {
            return AdminErrors.InvalidNotes();
        }

        var item = await admin.GetReviewItemAsync(id, ct);
        if (item is null)
        {
            return AdminErrors.ReviewItemNotFound(id);
        }

        if (item.Status != ReviewStatus.Open)
        {
            return AdminErrors.ReviewItemNotOpen();
        }

        if (request.Action != ReviewAction.Dismiss)
        {
            if (item.Bursary is null)
            {
                return AdminErrors.NoBursary();
            }

            if (request.Action == ReviewAction.Publish)
            {
                item.Bursary.IsPublished = true;
            }
            else
            {
                item.Bursary.Status = BursaryStatus.Closed;
                Lock(item.Bursary, nameof(Bursary.Status));
            }
        }

        var status = request.Action == ReviewAction.Dismiss ? ReviewStatus.Dismissed : ReviewStatus.Resolved;
        item.Status = status;
        item.ResolvedByUserId = userId;
        item.ResolvedAt = clock.UtcNow;
        item.Notes = request.Notes?.Trim() ?? string.Empty;
        if (item.ListingReport is { } report)
        {
            report.Status = status;
        }

        await admin.SaveChangesAsync(ct);
        return ToResponse(item);
    }

    public async Task<Result<PagedResponse<AdminBursarySummaryResponse>>> SearchBursariesAsync(
        string? query, bool? published, int page, CancellationToken ct)
    {
        if (page < 1)
        {
            return AdminErrors.InvalidPage();
        }

        var normalized = NameNormalizer.Normalize(query);
        var (items, total) = await admin.SearchBursariesAsync(normalized, published, (page - 1) * PageSize, PageSize, ct);
        return new PagedResponse<AdminBursarySummaryResponse>
        {
            Items = [.. items.Select(b => new AdminBursarySummaryResponse
            {
                Id = b.Id,
                Slug = b.Slug,
                Name = b.Name,
                FunderName = b.Funder?.Name,
                IsPublished = b.IsPublished,
                Status = b.Status.ToString(),
                ClosingDate = b.ClosingDate,
            })],
            Page = page,
            PageSize = PageSize,
            TotalCount = total,
        };
    }

    public async Task<Result<AdminBursaryResponse>> GetBursaryAsync(Guid id, CancellationToken ct)
    {
        var bursary = await admin.GetBursaryAsync(id, ct);
        return bursary is null ? AdminErrors.BursaryNotFound(id) : await ToDetailAsync(bursary, ct);
    }

    public async Task<Result<AdminBursaryResponse>> UpdateBursaryAsync(
        Guid id, IReadOnlyDictionary<string, JsonElement> changes, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(changes);

        var bursary = await admin.GetBursaryForUpdateAsync(id, ct);
        if (bursary is null)
        {
            return AdminErrors.BursaryNotFound(id);
        }

        var errors = new List<Error>();
        foreach (var (key, value) in changes)
        {
            try
            {
                var error = await ApplyAsync(bursary, key, value, ct);
                if (error is not null)
                {
                    errors.Add(error);
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
            {
                errors.Add(AdminErrors.InvalidField(key, "wrong type or format"));
            }
        }

        if (errors.Count > 0)
        {
            return Result<AdminBursaryResponse>.Failure([.. errors]);
        }

        // shortcut: editing closingDate does not recompute Status; the next ingestion run or the closure job does. Upgrade if admins need it instantly.
        await admin.SaveChangesAsync(ct);
        return await ToDetailAsync(bursary, ct);
    }

    public async Task<Result<AdminBursaryResponse>> UnlockFieldAsync(Guid id, string field, CancellationToken ct)
    {
        var bursary = await admin.GetBursaryForUpdateAsync(id, ct);
        if (bursary is null)
        {
            return AdminErrors.BursaryNotFound(id);
        }

        if (!bursary.IsLocked(field))
        {
            return AdminErrors.LockNotFound(field);
        }

        bursary.LockedFields = [.. bursary.LockedFields.Where(f => !string.Equals(f, field, StringComparison.OrdinalIgnoreCase))];
        await admin.SaveChangesAsync(ct);
        return await ToDetailAsync(bursary, ct);
    }

    /// <summary>Sets one field from its JSON value, locking it only if the value actually changed.</summary>
    private async Task<Error?> ApplyAsync(Bursary b, string key, JsonElement v, CancellationToken ct)
    {
        switch (key.ToLowerInvariant())
        {
            case "name":
            {
                var name = v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : string.Empty;
                if (name.Length is 0 or > 300)
                {
                    return AdminErrors.InvalidField(key, "required, 300 characters or fewer");
                }

                if (name != b.Name)
                {
                    b.Name = name;
                    b.NormalizedName = NameNormalizer.Normalize(name);
                    Lock(b, nameof(Bursary.Name));
                }

                return null;
            }

            case "summary":
                return SetText(b, key, v, 600, nameof(Bursary.Summary), b.Summary, x => b.Summary = x);

            case "applyurl":
                if (v.ValueKind == JsonValueKind.String
                    && !(Uri.TryCreate(v.GetString(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"))
                {
                    return AdminErrors.InvalidField(key, "must be an http(s) URL");
                }

                return SetText(b, key, v, 1000, nameof(Bursary.ApplyUrl), b.ApplyUrl, x => b.ApplyUrl = x);

            case "closingdate":
                return SetValue(b, nameof(Bursary.ClosingDate), v.Deserialize<DateOnly?>(), b.ClosingDate, x => b.ClosingDate = x);

            case "opendate":
                return SetValue(b, nameof(Bursary.OpenDate), v.Deserialize<DateOnly?>(), b.OpenDate, x => b.OpenDate = x);

            case "minaverage":
                var average = v.Deserialize<decimal?>();
                if (average is < 0 or > 100)
                {
                    return AdminErrors.InvalidField(key, "must be between 0 and 100");
                }

                return SetValue(b, nameof(Bursary.MinAverage), average, b.MinAverage, x => b.MinAverage = x);

            case "studylevels":
                return ParseEnums<StudyLevel>(v) is { } levels
                    ? SetSet(b, nameof(Bursary.StudyLevels), levels, b.StudyLevels, x => b.StudyLevels = x)
                    : AdminErrors.InvalidField(key, "must be a list of valid study levels");

            case "coverage":
                return ParseEnums<CoverageItem>(v) is { } coverage
                    ? SetSet(b, nameof(Bursary.Coverage), coverage, b.Coverage, x => b.Coverage = x)
                    : AdminErrors.InvalidField(key, "must be a list of valid coverage items");

            case "fundername":
                return await SetFunderAsync(b, key, v, ct);

            case "fieldcodes":
                return await SetFieldsAsync(b, key, v, ct);

            default:
                return AdminErrors.UnknownField(key);
        }
    }

    private static Error? SetText(Bursary b, string key, JsonElement v, int max, string lockName, string? current, Action<string?> set)
    {
        if (v.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
        {
            return AdminErrors.InvalidField(key, "must be text or null");
        }

        var text = v.GetString()?.Trim();
        text = string.IsNullOrEmpty(text) ? null : text;
        if (text?.Length > max)
        {
            return AdminErrors.InvalidField(key, $"{max} characters or fewer");
        }

        return SetValue(b, lockName, text, current, set);
    }

    private static Error? SetValue<T>(Bursary b, string lockName, T value, T current, Action<T> set)
    {
        if (!EqualityComparer<T>.Default.Equals(value, current))
        {
            set(value);
            Lock(b, lockName);
        }

        return null;
    }

    private static Error? SetSet<T>(Bursary b, string lockName, List<T> value, List<T> current, Action<List<T>> set)
    {
        if (!value.ToHashSet().SetEquals(current))
        {
            set(value);
            Lock(b, lockName);
        }

        return null;
    }

    private async Task<Error?> SetFunderAsync(Bursary b, string key, JsonElement v, CancellationToken ct)
    {
        if (v.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
        {
            return AdminErrors.InvalidField(key, "must be text or null");
        }

        var name = v.GetString()?.Trim();
        if (name?.Length > 200)
        {
            return AdminErrors.InvalidField(key, "200 characters or fewer");
        }

        var normalized = NameNormalizer.Normalize(name);
        Funder? funder = null;
        if (normalized.Length > 0)
        {
            funder = await funders.GetByNormalizedNameAsync(normalized, ct);
            if (funder is null)
            {
                funder = new Funder { Name = name!, NormalizedName = normalized };
                funders.Add(funder);
            }
        }

        if (funder?.Id != b.FunderId)
        {
            b.Funder = funder;
            b.FunderId = funder?.Id;
            Lock(b, nameof(Bursary.FunderId));
        }

        return null;
    }

    private async Task<Error?> SetFieldsAsync(Bursary b, string key, JsonElement v, CancellationToken ct)
    {
        var raw = v.Deserialize<List<string?>>();
        if (raw is null || raw.Any(string.IsNullOrWhiteSpace))
        {
            return AdminErrors.InvalidField(key, "must be a list of field-of-study codes");
        }

        var codes = raw.Select(c => c!.Trim()).Distinct(StringComparer.Ordinal).ToList();

        var ids = (await bursaryWrites.GetFieldIdsByCodesAsync(codes, ct)).ToHashSet();
        if (ids.Count != codes.Count)
        {
            return AdminErrors.InvalidField(key, "contains an unknown field-of-study code");
        }

        if (ids.SetEquals(b.Fields.Select(f => f.FieldOfStudyId)))
        {
            return null;
        }

        foreach (var stale in b.Fields.Where(f => !ids.Contains(f.FieldOfStudyId)).ToList())
        {
            b.Fields.Remove(stale);
        }

        var have = b.Fields.Select(f => f.FieldOfStudyId).ToHashSet();
        foreach (var fieldId in ids.Where(i => !have.Contains(i)))
        {
            b.Fields.Add(new BursaryField { BursaryId = b.Id, FieldOfStudyId = fieldId });
        }

        Lock(b, EligibilityLock);
        return null;
    }

    private static List<T>? ParseEnums<T>(JsonElement v)
        where T : struct, Enum
    {
        if (v.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var result = new List<T>();
        foreach (var element in v.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String
                || !Enum.TryParse<T>(element.GetString(), ignoreCase: true, out var parsed)
                || !Enum.IsDefined(parsed))
            {
                return null;
            }

            result.Add(parsed);
        }

        return result;
    }

    private static void Lock(Bursary b, string field)
    {
        if (!b.IsLocked(field))
        {
            b.LockedFields = [.. b.LockedFields, field];
        }
    }

    private static ReviewItemResponse ToResponse(ReviewItem r) => new()
    {
        Id = r.Id,
        Type = r.Type.ToString(),
        Status = r.Status.ToString(),
        CreatedAt = r.CreatedAt,
        BursaryId = r.BursaryId,
        BursaryName = r.Bursary?.Name,
        FunderName = r.Bursary?.Funder?.Name,
        IsPublished = r.Bursary?.IsPublished,
        BursaryStatus = r.Bursary?.Status.ToString(),
        ClosingDate = r.Bursary?.ClosingDate,
        Reason = r.Details?.RootElement.Clone(),
        ReportReason = r.ListingReport?.Reason.ToString(),
        ReportComment = r.ListingReport?.Comment,
    };

    private async Task<AdminBursaryResponse> ToDetailAsync(Bursary b, CancellationToken ct) => new()
    {
        Id = b.Id,
        Slug = b.Slug,
        Name = b.Name,
        FunderName = b.Funder?.Name,
        Summary = b.Summary,
        OpenDate = b.OpenDate,
        ClosingDate = b.ClosingDate,
        StudyLevels = [.. b.StudyLevels.Select(l => l.ToString())],
        MinAverage = b.MinAverage,
        Coverage = [.. b.Coverage.Select(c => c.ToString())],
        FieldCodes = await admin.GetFieldCodesAsync(b.Fields.Select(f => f.FieldOfStudyId), ct),
        ApplyUrl = b.ApplyUrl,
        IsPublished = b.IsPublished,
        Status = b.Status.ToString(),
        LockedFields = [.. b.LockedFields],
    };
}
