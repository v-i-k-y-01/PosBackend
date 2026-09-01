using PosBackend.Domain.Common;

namespace PosBackend.Domain.Entities;

/// <summary>
/// Represents a product or item offered in the Point-of-Sale system.
/// Contains pricing, inventory count, and unique SKU details.
/// </summary>
public class Product : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the category associated with this product.
    /// Can be null if the product is uncategorized.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the category this product belongs to.
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// Gets or sets the name of the product.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique Stock Keeping Unit (SKU) identifier of the product.
    /// </summary>
    public string Sku { get; set; } = null!;

    /// <summary>
    /// Gets or sets the price of the product (precision of 18,2).
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the current stock quantity available for sale.
    /// </summary>
    public int StockQty { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the product was added to the inventory (stored in UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the store this product belongs to.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the store this product belongs to.
    /// </summary>
    public Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of historical sale items referencing this product.
    /// Products with historical sale records are protected against hard deletion.
    /// </summary>
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
