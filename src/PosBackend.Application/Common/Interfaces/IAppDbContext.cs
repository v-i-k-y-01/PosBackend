using Microsoft.EntityFrameworkCore;
using PosBackend.Domain.Entities;

namespace PosBackend.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the database context, implemented by Infrastructure (AppDbContext).
/// Application and API code depend on this interface, never on the concrete DbContext.
/// </summary>
public interface IAppDbContext
{
    /// <summary>
    /// Gets the database set for Users.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Gets the database set for Categories.
    /// </summary>
    DbSet<Category> Categories { get; }

    /// <summary>
    /// Gets the database set for Products.
    /// </summary>
    DbSet<Product> Products { get; }

    /// <summary>
    /// Gets the database set for Sales.
    /// </summary>
    DbSet<Sale> Sales { get; }

    /// <summary>
    /// Gets the database set for SaleItems.
    /// </summary>
    DbSet<SaleItem> SaleItems { get; }

    /// <summary>
    /// Persists all changes made in this context to the underlying database.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a series of database operations inside an isolated transaction boundary.
    /// Transaction commits automatically if the operation completes successfully; otherwise, rolls back.
    /// </summary>
    /// <typeparam name="T">The return type of the transactional operation.</typeparam>
    /// <param name="operation">The delegate executing database operations, taking a cancellation token.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the transaction execution containing the operation results.</returns>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
