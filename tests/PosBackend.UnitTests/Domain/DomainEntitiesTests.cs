using FluentAssertions;
using PosBackend.Domain.Entities;
using PosBackend.Domain.Enums;
using Xunit;

namespace PosBackend.UnitTests.Domain;

public class DomainEntitiesTests
{
    [Fact]
    public void BaseEntity_ShouldInitializeWithNewGuid()
    {
        // Act
        var category = new Category();

        // Assert
        category.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void User_ShouldCorrectlyConstructAndHoldValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "admin@pos.com";
        var passwordHash = "hash123";
        var role = UserRole.Owner;
        var createdAt = DateTime.UtcNow;

        // Act
        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = createdAt
        };

        // Assert
        user.Id.Should().Be(userId);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.Role.Should().Be(role);
        user.CreatedAt.Should().Be(createdAt);
        user.Sales.Should().BeEmpty();
    }

    [Fact]
    public void SaleItem_ShouldCorrectlySetProperties()
    {
        // Arrange
        var saleItem = new SaleItem
        {
            Quantity = 3,
            UnitPrice = 150.50m,
            Subtotal = 451.50m
        };

        // Act & Assert
        saleItem.Subtotal.Should().Be(451.50m);
        saleItem.Quantity.Should().Be(3);
        saleItem.UnitPrice.Should().Be(150.50m);
    }

    [Fact]
    public void Sale_ShouldAccumulateTotalAmountFromItems()
    {
        // Arrange
        var sale = new Sale
        {
            PaymentMethod = PaymentMethod.Card
        };

        var item1 = new SaleItem { Quantity = 2, UnitPrice = 10.00m, Subtotal = 20.00m };
        var item2 = new SaleItem { Quantity = 1, UnitPrice = 50.00m, Subtotal = 50.00m };

        // Act
        sale.Items.Add(item1);
        sale.Items.Add(item2);
        sale.TotalAmount = sale.Items.Sum(i => i.Subtotal);

        // Assert
        sale.TotalAmount.Should().Be(70.00m);
        sale.PaymentMethod.Should().Be(PaymentMethod.Card);
    }

    [Fact]
    public void Sale_ShouldSupportUpiPaymentMethod()
    {
        // Arrange & Act
        var sale = new Sale
        {
            PaymentMethod = PaymentMethod.Upi
        };

        // Assert
        sale.PaymentMethod.Should().Be(PaymentMethod.Upi);
        sale.PaymentMethod.ToString().Should().Be("Upi");
    }
}
