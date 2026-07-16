using PosBackend.Domain.Common;

namespace PosBackend.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = null!;

    // Navigation: products grouped under this category.
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
