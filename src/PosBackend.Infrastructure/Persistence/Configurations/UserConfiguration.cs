using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosBackend.Domain.Entities;

namespace PosBackend.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the User domain entity.
/// Defines table mappings, keys, constraints, column conversions, unique indexes, and relationships.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Configures constraints, column properties, conversions, and relational bounds for the Users table.
    /// </summary>
    /// <param name="builder">The builder to configure User entity details.</param>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(user => user.Email).IsUnique();

        builder.Property(user => user.StoreId)
            .IsRequired();

        builder.HasOne(user => user.Store)
            .WithMany(store => store.Users)
            .HasForeignKey(user => user.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(user => user.PasswordHash).IsRequired();

        // Store the enum as its string name ("Owner"/"Cashier") for readability.
        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(user => user.CreatedAt).IsRequired();

        builder.HasMany(user => user.Sales)
            .WithOne(sale => sale.Cashier)
            .HasForeignKey(sale => sale.CashierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
