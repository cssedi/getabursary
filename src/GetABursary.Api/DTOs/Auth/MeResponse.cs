namespace GetABursary.Api.DTOs.Auth;

public record MeResponse
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required IReadOnlyList<string> Roles { get; init; }
}
