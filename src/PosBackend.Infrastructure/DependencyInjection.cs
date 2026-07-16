using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Infrastructure.Persistence;

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

        return services;
    }
}
