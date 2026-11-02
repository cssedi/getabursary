using GetABursary.Core.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Extensions;

/// <summary>One entry of the <c>errors</c> extension on a failure <see cref="ProblemDetails"/>.</summary>
public sealed record ProblemError(string Code, string Message);

/// <summary>Converts a <see cref="Result{T}"/> from a service into the matching HTTP response.</summary>
public static class ResultExtensions
{
    private const string ErrorsExtensionKey = "errors";

    /// <summary>Success returns <c>200 OK</c> with the value; failure maps the first error's type to a status code.</summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.ToActionResult(controller, _ => StatusCodes.Status200OK);

    /// <summary>
    /// As above, but the success status code is chosen from the value (e.g. <c>201 Created</c>).
    /// Failures are returned as <see cref="ProblemDetails"/>.
    /// </summary>
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, int> successStatusCode)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(successStatusCode);

        if (result.IsSuccess)
        {
            var value = result.Value;
            return controller.StatusCode(successStatusCode(value), value);
        }

        var first = result.Errors[0];
        var status = MapStatusCode(first.Type);

        var problem = new ProblemDetails
        {
            Title = first.Message,
            Status = status,
        };
        problem.Extensions[ErrorsExtensionKey] = result.Errors
            .Select(error => new ProblemError(error.Code, error.Message))
            .ToList();

        return controller.StatusCode(status, problem);
    }

    private static int MapStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Gone => StatusCodes.Status410Gone,
        ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown error type."),
    };
}
