using GetABursary.Core.Common;

namespace GetABursary.Core.Tests.Common;

public class ResultTests
{
    private static readonly Error NotFoundError = Error.NotFound("Bursary.NotFound", "Bursary was not found");

    [Fact]
    public void Success_WithValue_IsSuccessAndExposesValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WithErrors_IsFailureAndExposesErrors()
    {
        var result = Result<int>.Failure(NotFoundError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal([NotFoundError], result.Errors);
    }

    [Fact]
    public void Failure_WithMultipleErrors_KeepsAllErrorsInOrder()
    {
        var second = Error.Validation("Bursary.Name", "Name is required");

        var result = Result<int>.Failure(NotFoundError, second);

        Assert.Equal([NotFoundError, second], result.Errors);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailure()
    {
        Result<string> result = NotFoundError;

        Assert.True(result.IsFailure);
        Assert.Equal([NotFoundError], result.Errors);
    }

    [Fact]
    public void Value_OnFailure_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Failure(NotFoundError);

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Failure_WithNoErrors_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result<int>.Failure());
    }
}
