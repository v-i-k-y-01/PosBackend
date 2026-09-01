using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosBackend.Domain.Entities;

namespace PosBackend.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Store domain entity.
/// Defines table mappings, keys, constraints, and relationships.
/// </summary>
public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    /// <summary>
    /// Configures constraints, column properties, and relational bounds for the Stores table.
    /// </summary>
    /// <param name="builder">The builder to configure Store entity details.</param>
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores");
        builder.HasKey(store => store.Id);

        builder.Property(store => store.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(store => store.CreatedAt)
            .IsRequired();
    }
}
