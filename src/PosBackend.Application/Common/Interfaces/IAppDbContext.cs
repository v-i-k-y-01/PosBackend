using Microsoft.EntityFrameworkCore;
using PosBackend.Domain.Entities;

namespace PosBackend.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the database context, implemented by Infrastructure (AppDbContext).
/// Application and API code depend on this interface, never on the concrete DbContext.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
