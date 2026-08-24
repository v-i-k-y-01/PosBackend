using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosBackend.Domain.Entities;

namespace PosBackend.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Product domain entity.
/// Defines table mappings, keys, constraints, indexes, and relationships.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>
    /// Configures constraints, column properties, indexes, and relational bounds for the Products table.
    /// </summary>
    /// <param name="builder">The builder to configure Product entity details.</param>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(product => product.Sku)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(product => product.Sku).IsUnique();

        builder.Property(product => product.Price)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(product => product.StockQty).IsRequired();

        builder.Property(product => product.CreatedAt).IsRequired();

        // Don't delete a product that has historical sale items; restrict instead.
        builder.HasMany(product => product.SaleItems)
            .WithOne(saleItem => saleItem.Product)
            .HasForeignKey(saleItem => saleItem.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
