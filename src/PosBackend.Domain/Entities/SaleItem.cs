using PosBackend.Domain.Common;

namespace PosBackend.Domain.Entities;

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    // Snapshot of the product's price at the time of sale.
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}
