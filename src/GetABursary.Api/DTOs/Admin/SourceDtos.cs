namespace GetABursary.Api.DTOs.Admin;

/// <summary>Body of <c>PATCH /api/v1/admin/sources/{id}</c>. Nullable so a missing field is rejected rather than read as false.</summary>
public sealed record UpdateSourceRequest
{
    public bool? Enabled { get; init; }
}

/// <summary>Body of <c>POST /api/v1/admin/ingestion-runs</c>. Null <see cref="SourceId"/> means all enabled sources.</summary>
public sealed record CreateIngestionRunRequest
{
    public Guid? SourceId { get; init; }
}

public sealed record SourceResponse
{
    public required Guid Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    public required bool Enabled { get; init; }

    public LastRunResponse? LastRun { get; init; }
}

public sealed record LastRunResponse
{
    public required DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    public required string Status { get; init; }

    public required int Discovered { get; init; }

    public required int Created { get; init; }

    public required int Updated { get; init; }

    public required int Failed { get; init; }

    public string? ErrorSummary { get; init; }
}

public sealed record IngestionRunRequestResponse
{
    public required Guid Id { get; init; }

    public Guid? SourceId { get; init; }

    public required DateTimeOffset RequestedAt { get; init; }

    public DateTimeOffset? PickedUpAt { get; init; }
}

public sealed record IngestionRunResponse
{
    public required Guid Id { get; init; }

    public required string Trigger { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    public required IReadOnlyList<RunSourceResponse> Sources { get; init; }
}

public sealed record RunSourceResponse
{
    public required Guid SourceId { get; init; }

    public required string SourceKey { get; init; }

    public required string SourceName { get; init; }

    public required int Discovered { get; init; }

    public required int Created { get; init; }

    public required int Updated { get; init; }

    public required int Failed { get; init; }

    public string? ErrorSummary { get; init; }
}
