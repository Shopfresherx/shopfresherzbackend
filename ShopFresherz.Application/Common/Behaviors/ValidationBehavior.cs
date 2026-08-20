using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;

namespace ShopFresherz.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behaviour that runs all registered <see cref="IValidator{T}"/> instances
/// before the handler. Returns a validation <see cref="Error"/> result instead of throwing.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">Must be <c>Result&lt;T&gt;</c> for this pipeline to apply.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>Initialises the behavior with all registered validators for <typeparamref name="TRequest"/>.</summary>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        ValidationContext<TRequest> context = new(request);

        IEnumerable<FluentValidation.Results.ValidationFailure> failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (!failures.Any())
        {
            return await next();
        }

        string message = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // Construct a failure result via reflection so this behavior works for any Result<T>.
        Type responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            Error error = Error.Validation(message);
            System.Reflection.MethodInfo? failureMethod = responseType.GetMethod(
                "Failure",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (failureMethod is not null)
            {
                object? result = failureMethod.Invoke(null, new object[] { error });
                if (result is TResponse typedResult)
                {
                    return typedResult;
                }
            }
        }

        // Fallback: throw if response is not Result<T>.
        throw new ValidationException(failures);
    }
}
