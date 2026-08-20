namespace ShopFresherz.Application.Common;

/// <summary>
/// Discriminated union result type used by all Application layer handlers.
/// Handlers return <c>Result&lt;T&gt;</c> instead of throwing exceptions for expected failures.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
public sealed class Result<T>
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the success value. Only valid when <see cref="IsSuccess"/> is <c>true</c>.</summary>
    public T? Value { get; }

    /// <summary>Gets the failure error. Only valid when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    public Error Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = Error.None;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    /// <summary>Creates a successful result wrapping <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Creates a failure result from <paramref name="error"/>.</summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>Implicit conversion from <typeparamref name="T"/> to a success result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Implicit conversion from <see cref="Error"/> to a failure result.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}

/// <summary>
/// Represents an application-layer error with a machine-readable code and human-readable message.
/// </summary>
/// <param name="Code">Machine-readable error code (e.g., NOT_FOUND, CONFLICT).</param>
/// <param name="Message">Human-readable error description.</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>Sentinel value for the absence of an error.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Creates a NOT_FOUND error for the given resource name.</summary>
    public static Error NotFound(string resource) =>
        new("NOT_FOUND", $"{resource} was not found.");

    /// <summary>Creates a CONFLICT error.</summary>
    public static Error Conflict(string message) =>
        new("CONFLICT", message);

    /// <summary>Creates an UNAUTHORIZED error.</summary>
    public static Error Unauthorized(string message = "You are not authorised to perform this action.") =>
        new("UNAUTHORIZED", message);

    /// <summary>Creates a FORBIDDEN error.</summary>
    public static Error Forbidden(string message = "Access is forbidden.") =>
        new("FORBIDDEN", message);

    /// <summary>Creates a VALIDATION error.</summary>
    public static Error Validation(string message) =>
        new("VALIDATION", message);

    /// <summary>Creates an INTERNAL error.</summary>
    public static Error Internal(string message = "An unexpected error occurred.") =>
        new("INTERNAL", message);

    /// <summary>Creates a custom error with the supplied code and message.</summary>
    public static Error Custom(string code, string message) =>
        new(code, message);
}
