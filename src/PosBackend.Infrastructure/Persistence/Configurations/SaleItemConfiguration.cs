using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosBackend.Domain.Entities;

namespace PosBackend.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the SaleItem domain entity.
/// Defines table mappings, keys, constraints, and column data types.
/// </summary>
public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    /// <summary>
    /// Configures constraints, column properties, and types for the SaleItems table.
    /// </summary>
    /// <param name="builder">The builder to configure SaleItem entity details.</param>
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");
        builder.HasKey(saleItem => saleItem.Id);

        builder.Property(saleItem => saleItem.Quantity).IsRequired();

        builder.Property(saleItem => saleItem.UnitPrice)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(saleItem => saleItem.Subtotal)
            .HasColumnType("numeric(18,2)")
            .IsRequired();
    }
}
