using PosBackend.Domain.Common;
using PosBackend.Domain.Enums;

namespace PosBackend.Domain.Entities;

/// <summary>
/// Represents a transactional sale record containing order details, payment method, 
/// transaction total, and associated line items.
/// </summary>
public class Sale : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the cashier who rung up this sale.
    /// </summary>
    public Guid CashierId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the cashier who rung up this sale.
    /// </summary>
    public User? Cashier { get; set; }

    /// <summary>
    /// Gets or sets the total transaction amount of this sale.
    /// Calculated dynamically as the sum of subtotals of all individual sale items.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the payment method used for this sale transaction (Cash, Card, or UPI).
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the sale was recorded (stored in UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the store where this sale occurred.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the store where this sale occurred.
    /// </summary>
    public Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the collection of line items that comprise this sale.
    /// Deleting a sale cascades and deletes all associated sale line items.
    /// </summary>
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
