using System.Text.Json.Serialization;
using GetABursary.Api.DTOs.Bursaries;
using GetABursary.Core.Domain.Users;

namespace GetABursary.Api.DTOs.Tracker;

/// <summary>One saved bursary in the student's tracker. <c>Status</c> is serialised as its name.</summary>
public sealed record TrackerEntryResponse
{
    public required BursarySummaryResponse Bursary { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required TrackerStatus Status { get; init; }

    public DateOnly? AppliedOn { get; init; }

    public string? Notes { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
