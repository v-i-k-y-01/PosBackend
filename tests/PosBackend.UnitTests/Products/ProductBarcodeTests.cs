using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Application.Products;
using PosBackend.Domain.Entities;
using PosBackend.Infrastructure.Persistence;
using PosBackend.UnitTests.Common;
using Xunit;

namespace PosBackend.UnitTests.Products;

public class ProductBarcodeTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"PosTestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_GetProductByBarcode_ShouldReturnProduct_WhenBarcodeExists()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUserService = new TestCurrentUserService();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            StoreId = currentUserService.StoreId,
            Name = "Basmati Rice 1kg",
            Sku = "890123456789",
            Price = 149.00m,
            StockQty = 50,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var handler = new ProductHandlers(dbContext, currentUserService);
        var query = new GetProductByBarcodeQuery("890123456789");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Basmati Rice 1kg");
        result.Sku.Should().Be("890123456789");
        result.Price.Should().Be(149.00m);
    }

    [Fact]
    public async Task Handle_GetProductByBarcode_ShouldThrowNotFoundException_WhenBarcodeDoesNotExist()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUserService = new TestCurrentUserService();
        var handler = new ProductHandlers(dbContext, currentUserService);
        var query = new GetProductByBarcodeQuery("999999999999");

        // Act
        var act = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_GenerateBarcode_ShouldReturn12DigitStandardBarcode()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUserService = new TestCurrentUserService();
        var handler = new ProductHandlers(dbContext, currentUserService);
        var query = new GenerateBarcodeQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Barcode.Should().HaveLength(12);
        result.Barcode.Should().StartWith("890");
    }
}
