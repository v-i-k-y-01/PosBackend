using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PosBackend.Application.Common.Behaviors;

namespace PosBackend.Application;

/// <summary>
/// Registers Application-layer services: MediatR (with the validation pipeline behavior),
/// AutoMapper profile scanning, and FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Extension method to register all Application-layer dependencies in the service collection.
    /// This includes MediatR handlers, pipeline behaviors, AutoMapper profiles, and FluentValidation validators.
    /// </summary>
    /// <param name="services">The DI container service collection to register services to.</param>
    /// <returns>The modified service collection for chaining calls.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddAutoMapper(cfg => { }, typeof(DependencyInjection).Assembly);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
