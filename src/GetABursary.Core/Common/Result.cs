using System.Diagnostics.CodeAnalysis;

namespace GetABursary.Core.Common;

/// <summary>
/// Outcome of a service call: either a value or one or more expected <see cref="Error"/>s.
/// Services return this for expected failures; unexpected failures are thrown.
/// </summary>
[SuppressMessage("Design", "CA1000", Justification = "Static factories Success/Failure are mandated by specs/03-conventions.md section 2.")]
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        Errors = [];
    }

    private Result(IReadOnlyList<Error> errors)
    {
        IsSuccess = false;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary>The success value. Throws <see cref="InvalidOperationException"/> on failure.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed Result. Check IsSuccess first.");

    public IReadOnlyList<Error> Errors { get; }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(params Error[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Length == 0)
        {
            throw new ArgumentException("A failed Result must contain at least one error.", nameof(errors));
        }

        return new Result<T>(errors.ToArray());
    }

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
