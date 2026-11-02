using System.Text.Json;
using GetABursary.Api.DTOs.Admin;
using GetABursary.Api.DTOs.Common;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Api.Services.Admin;

public interface IAdminService
{
    Task<Result<PagedResponse<ReviewItemResponse>>> ListReviewItemsAsync(ReviewItemType? type, int page, CancellationToken ct);

    /// <summary>Resolves an open review item (and publishes or closes its bursary) in one transaction.</summary>
    Task<Result<ReviewItemResponse>> ResolveReviewItemAsync(Guid id, Guid userId, ResolveReviewItemRequest request, CancellationToken ct);

    Task<Result<PagedResponse<AdminBursarySummaryResponse>>> SearchBursariesAsync(string? query, bool? published, int page, CancellationToken ct);

    Task<Result<AdminBursaryResponse>> GetBursaryAsync(Guid id, CancellationToken ct);

    /// <summary>Applies any subset of the editable fields (JSON property name to value) and locks each field that changed.</summary>
    Task<Result<AdminBursaryResponse>> UpdateBursaryAsync(Guid id, IReadOnlyDictionary<string, JsonElement> changes, CancellationToken ct);

    Task<Result<AdminBursaryResponse>> UnlockFieldAsync(Guid id, string field, CancellationToken ct);
}
