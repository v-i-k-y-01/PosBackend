using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosBackend.Domain.Entities;

namespace PosBackend.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Sale domain entity.
/// Defines table mappings, keys, constraints, column conversions, and relationships.
/// </summary>
public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    /// <summary>
    /// Configures constraints, column conversions, and relational bounds for the Sales table.
    /// </summary>
    /// <param name="builder">The builder to configure Sale entity details.</param>
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.HasKey(sale => sale.Id);

        builder.Property(sale => sale.TotalAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(sale => sale.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(sale => sale.CreatedAt).IsRequired();

        // Deleting a sale cascades to its line items.
        builder.HasMany(sale => sale.Items)
            .WithOne(saleItem => saleItem.Sale)
            .HasForeignKey(saleItem => saleItem.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
