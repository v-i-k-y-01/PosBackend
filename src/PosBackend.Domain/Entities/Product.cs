using PosBackend.Domain.Common;

namespace PosBackend.Domain.Entities;

public class Product : BaseEntity
{
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Name { get; set; } = null!;
    public string Sku { get; set; } = null!;

    public decimal Price { get; set; }
    public int StockQty { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation: line items across sales that reference this product.
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
