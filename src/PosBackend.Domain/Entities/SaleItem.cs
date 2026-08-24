using PosBackend.Domain.Common;

namespace PosBackend.Domain.Entities;

/// <summary>
/// Represents a specific line item in a sale transaction.
/// Records the product quantity and captures a historical snapshot of the unit price at checkout.
/// </summary>
public class SaleItem : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the sale associated with this item.
    /// </summary>
    public Guid SaleId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the parent sale transaction.
    /// </summary>
    public Sale? Sale { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the product sold.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the product sold.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Gets or sets the quantity of the product purchased in this transaction.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price of the product at the time of the sale.
    /// Acts as a snapshot to protect history from future product price changes.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the subtotal for this line item.
    /// Calculated as Quantity * UnitPrice.
    /// </summary>
    public decimal Subtotal { get; set; }
}
