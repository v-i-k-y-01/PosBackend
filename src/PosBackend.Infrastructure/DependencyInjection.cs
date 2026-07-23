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
/// Registers Infrastructure-layer services. For Step 1 this is just the EF Core
/// DbContext bound to PostgreSQL via ConnectionStrings:DefaultConnection, exposed
/// through the IAppDbContext abstraction. Auth/token services are added in Step 2.
/// </summary>
public static class DependencyInjection
{
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
