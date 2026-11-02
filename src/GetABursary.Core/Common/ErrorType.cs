namespace GetABursary.Core.Common;

/// <summary>Category of an expected failure. Mapped to HTTP status codes by the API layer.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Gone,
    Unauthorized,
    Forbidden,
    Unexpected,
}
