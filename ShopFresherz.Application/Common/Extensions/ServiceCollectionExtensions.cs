using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ShopFresherz.Application.Common.Mappings;
using ShopFresherz.Application.Features.Payments.Services;
using System.Reflection;

namespace ShopFresherz.Application.Common.Extensions;

/// <summary>Extension methods for registering Application layer services in the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, AutoMapper, and the
    /// <see cref="ValidationBehavior{TRequest,TResponse}"/> pipeline behaviour.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(ServiceCollectionExtensions).Assembly;

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddAutoMapper(typeof(MappingProfile));
        services.AddScoped<IOrderPaymentConfirmationService, OrderPaymentConfirmationService>();

        return services;
    }
}
