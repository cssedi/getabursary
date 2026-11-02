using GetABursary.Api.DTOs.Bursaries;
using GetABursary.Api.DTOs.Common;
using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Bursaries;

public interface IBursaryMatchingService
{
    /// <summary>Validates the request, resolves the qualification into target fields and returns one ranked page of eligible bursaries.</summary>
    Task<Result<PagedResponse<BursarySummaryResponse>>> SearchAsync(BursarySearchRequest request, CancellationToken ct);
}
