using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosBackend.Domain.Entities;

namespace PosBackend.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Category domain entity.
/// Defines table mappings, keys, constraints, and relationships.
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>
    /// Configures constraints, column properties, and relational bounds for the Categories table.
    /// </summary>
    /// <param name="builder">The builder to configure Category entity details.</param>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Deleting a category nulls out the product's CategoryId (CategoryId is nullable).
        builder.HasMany(category => category.Products)
            .WithOne(product => product.Category)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
