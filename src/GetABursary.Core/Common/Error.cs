using System.Diagnostics.CodeAnalysis;

namespace GetABursary.Core.Common;

/// <summary>An expected failure. <c>Code</c> is a stable identifier such as <c>Bursary.NotFound</c>.</summary>
[SuppressMessage("Naming", "CA1716", Justification = "The type name Error is mandated by specs/03-conventions.md section 2.")]
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Gone(string code, string message) => new(code, message, ErrorType.Gone);
}
