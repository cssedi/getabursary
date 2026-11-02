using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GetABursary.Core.Domain.Users;

namespace GetABursary.Api.DTOs.Tracker;

public sealed record UpsertTrackerRequest
{
    /// <summary>Nullable so that a missing status is a validation error rather than silently defaulting to <c>Saved</c>.</summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TrackerStatus? Status { get; init; }

    public DateOnly? AppliedOn { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}
