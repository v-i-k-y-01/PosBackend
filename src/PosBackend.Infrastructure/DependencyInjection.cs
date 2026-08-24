using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Infrastructure.Authentication;
using PosBackend.Infrastructure.Persistence;
using PosBackend.Infrastructure.Services;

namespace PosBackend.Infrastructure;

/// <summary>
/// Registers Infrastructure-layer services. This includes EF Core DbContext,
/// database transaction management, BCrypt password hashing, JWT token services,
/// and context-based current user retrieval.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Extension method to register all Infrastructure-layer dependencies in the service collection.
    /// Wires up Entity Framework PostgreSQL connection, identity utilities, options, and HTTP Context accessor.
    /// </summary>
    /// <param name="services">The DI container service collection to register services to.</param>
    /// <param name="configuration">The configuration instance carrying connections and settings.</param>
    /// <returns>The modified service collection for chaining calls.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
