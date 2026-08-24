using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Domain.Entities;

namespace PosBackend.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context implementation for the application.
/// Manages entity mappings, database connections, and transactional boundary executions.
/// </summary>
public class AppDbContext : DbContext, IAppDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options configuration.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Gets the database set for Users.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets the database set for Categories.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Gets the database set for Products.
    /// </summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>
    /// Gets the database set for Sales.
    /// </summary>
    public DbSet<Sale> Sales => Set<Sale>();

    /// <summary>
    /// Gets the database set for SaleItems.
    /// </summary>
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    /// <summary>
    /// Executes the specified delegate operations inside a transaction block.
    /// Automatically commits if successful, otherwise rolls back changes on exception.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="operation">The delegate carrying database operations to execute.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            // Rollback transaction to ensure partial data is never committed.
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Configures EF Core model mappings. Applies all entity configurations from the current assembly.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply every IEntityTypeConfiguration<> found in this assembly (Persistence/Configurations).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
