using PosBackend.Domain.Common;

namespace PosBackend.Domain.Entities;

/// <summary>
/// Represents an isolated Store or Business tenant within the POS system.
/// Every user, category, product, and sales transaction is partitioned by Store.
/// </summary>
public class Store : BaseEntity
{
    /// <summary>
    /// Gets or sets the display name of the store.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the store was created (stored in UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the collection of user accounts (owners, cashiers) affiliated with this store.
    /// </summary>
    public ICollection<User> Users { get; set; } = new List<User>();

    /// <summary>
    /// Gets or sets the collection of categories defined within this store.
    /// </summary>
    public ICollection<Category> Categories { get; set; } = new List<Category>();

    /// <summary>
    /// Gets or sets the collection of products managed within this store's inventory.
    /// </summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();

    /// <summary>
    /// Gets or sets the collection of sales transactions completed at this store.
    /// </summary>
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
