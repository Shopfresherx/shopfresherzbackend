using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace ShopFresherz.Application.Common;

/// <summary>
/// MediatR pipeline behaviour that runs all registered FluentValidation
/// validators before the handler is invoked.
/// Returns a validation Error result instead of throwing exceptions.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        ValidationContext<TRequest> context = new(request);

        ValidationResult[] results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<ValidationFailure> failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0) return await next();

        string message = string.Join(" | ", failures.Select(f => f.ErrorMessage));

        Type responseType = typeof(TResponse);
        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            Error error = Error.Validation(message);
            System.Reflection.MethodInfo? implicitOp = responseType.GetMethod(
                "op_Implicit",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new[] { typeof(Error) },
                null);

            if (implicitOp is not null)
            {
                object? result = implicitOp.Invoke(null, new object[] { error });
                return (TResponse)result!;
            }
        }

        throw new ValidationException(failures);
    }
}
