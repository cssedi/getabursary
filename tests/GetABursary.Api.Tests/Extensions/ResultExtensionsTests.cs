using GetABursary.Api.Extensions;
using GetABursary.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Tests.Extensions;

public class ResultExtensionsTests
{
    private sealed class TestController : ControllerBase;

    private static readonly TestController Controller = new();

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Gone, 410)]
    [InlineData(ErrorType.Unexpected, 500)]
    public void ToActionResult_Failure_MapsErrorTypeToStatusCode(ErrorType type, int expectedStatus)
    {
        Result<string> result = new Error("Test.Code", "Something went wrong", type);

        var action = result.ToActionResult(Controller);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);
    }

    [Fact]
    public void ToActionResult_Failure_ProblemDetailsContainsTitleAndAllErrorCodes()
    {
        var result = Result<string>.Failure(
            Error.Validation("Bursary.Name", "Name is required"),
            Error.Validation("Bursary.Amount", "Amount must be positive"));

        var action = result.ToActionResult(Controller);

        var objectResult = Assert.IsType<ObjectResult>(action);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Name is required", problem.Title);
        var errors = Assert.IsAssignableFrom<IEnumerable<ProblemError>>(problem.Extensions["errors"]).ToList();
        Assert.Equal(["Bursary.Name", "Bursary.Amount"], errors.Select(e => e.Code));
        Assert.Equal(["Name is required", "Amount must be positive"], errors.Select(e => e.Message));
    }

    [Fact]
    public void ToActionResult_Success_ReturnsOkWithValue()
    {
        Result<string> result = "hello";

        var action = result.ToActionResult(Controller);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.Equal("hello", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_SuccessWithStatusSelector_ReturnsSelectedStatus()
    {
        Result<int> result = 7;

        var action = result.ToActionResult(Controller, _ => 201);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(201, objectResult.StatusCode);
        Assert.Equal(7, objectResult.Value);
    }
}
