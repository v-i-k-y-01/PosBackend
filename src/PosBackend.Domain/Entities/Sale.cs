using PosBackend.Domain.Common;
using PosBackend.Domain.Enums;

namespace PosBackend.Domain.Entities;

public class Sale : BaseEntity
{
    public Guid CashierId { get; set; }
    public User? Cashier { get; set; }

    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation: the line items that make up this sale.
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
