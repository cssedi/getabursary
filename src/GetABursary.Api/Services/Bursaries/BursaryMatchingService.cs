using GetABursary.Api.DTOs.Bursaries;
using GetABursary.Api.DTOs.Common;
using GetABursary.Core.Common;
using GetABursary.Core.Repositories.Bursaries;
using GetABursary.Core.Repositories.Taxonomy;

namespace GetABursary.Api.Services.Bursaries;

public sealed class BursaryMatchingService(
    IBursaryReadRepository bursaries,
    IQualificationResolutionRepository qualifications,
    IClock clock) : IBursaryMatchingService
{
    private const int ClosingSoonDays = 14;

    public async Task<Result<PagedResponse<BursarySummaryResponse>>> SearchAsync(BursarySearchRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = BursarySearchValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Result<PagedResponse<BursarySummaryResponse>>.Failure([.. errors]);
        }

        var fieldIds = new HashSet<Guid>(request.FieldIds ?? []);
        var institutionId = request.InstitutionId;

        if (request.QualificationId is { } qualificationId)
        {
            var resolution = await qualifications.ResolveAsync(qualificationId, ct);
            if (resolution is null)
            {
                return Result<PagedResponse<BursarySummaryResponse>>.Failure(BursarySearchErrors.UnknownQualification(qualificationId));
            }

            fieldIds.UnionWith(resolution.FieldIds);
            institutionId ??= resolution.InstitutionId;
        }

        var criteria = new BursarySearchCriteria
        {
            QualificationId = request.QualificationId,
            FieldIds = [.. fieldIds],
            InstitutionId = institutionId,
            StudyLevel = request.StudyLevel,
            Average = request.Average,
            ClosingFrom = request.ClosingFrom,
            ClosingTo = request.ClosingTo,
            IncludeClosed = request.IncludeClosed,
            Sort = request.Sort,
            Page = request.Page,
            PageSize = request.PageSize,
        };

        var page = await bursaries.SearchAsync(criteria, ct);
        var today = clock.Today;

        return Result<PagedResponse<BursarySummaryResponse>>.Success(new PagedResponse<BursarySummaryResponse>
        {
            Items = [.. page.Items.Select(row => ToResponse(row, today))],
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = page.TotalCount,
        });
    }

    private static BursarySummaryResponse ToResponse(BursarySearchRow row, DateOnly today) => new()
    {
        Id = row.Id,
        Slug = row.Slug,
        Name = row.Name,
        FunderName = row.FunderName,
        ClosingDate = row.ClosingDate,
        IsClosingSoon = row.Status == Core.Domain.Common.Enums.BursaryStatus.Open
            && row.ClosingDate is { } closing
            && closing >= today
            && closing <= today.AddDays(ClosingSoonDays),
        Status = row.Status.ToString(),
        StudyLevels = [.. row.StudyLevels.Select(l => l.ToString())],
        MinAverage = row.MinAverage,
        Coverage = [.. row.Coverage.Select(c => c.ToString())],
        Sources = [.. row.Sources.Select(s => new BursarySourceResponse
        {
            Key = s.Key,
            Name = s.Name,
            IconUrl = s.IconUrl,
            Url = s.Url,
        })],
        MatchScore = row.MatchScore,
    };
}
