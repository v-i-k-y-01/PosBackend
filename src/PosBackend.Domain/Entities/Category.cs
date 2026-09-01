using PosBackend.Domain.Common;

namespace PosBackend.Domain.Entities;

/// <summary>
/// Represents a category used to group and classify products in the system.
/// </summary>
public class Category : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique name of the category.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the store this category belongs to.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the store this category belongs to.
    /// </summary>
    public Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of products associated with this category.
    /// </summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
