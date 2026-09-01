using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PosBackend.Application.Auth.Commands;
using PosBackend.Application.Categories;
using PosBackend.Application.Common.Exceptions;
using PosBackend.Application.Common.Interfaces;
using PosBackend.Application.Products;
using PosBackend.Application.Reports;
using PosBackend.Application.Sales;
using PosBackend.Application.Users.Commands;
using PosBackend.Domain.Entities;
using PosBackend.Domain.Enums;
using PosBackend.Infrastructure.Persistence;
using PosBackend.UnitTests.Common;
using Xunit;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PosBackend.UnitTests.Tenancy;

public class MockPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed_{password}";
    public bool Verify(string password, string passwordHash) => passwordHash == $"hashed_{password}";
}

public class StoreIsolationTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"PosMultiTenantTest_{Guid.NewGuid()}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegisterOwner_ShouldCreateIsolatedStoreForNewOwner()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var hasher = new MockPasswordHasher();
        var handler = new RegisterOwnerCommandHandler(dbContext, hasher);

        // Act
        var response1 = await handler.Handle(new RegisterOwnerCommand("owner1@store1.com", "Password123!", "Store 1"), CancellationToken.None);
        var response2 = await handler.Handle(new RegisterOwnerCommand("owner2@store2.com", "Password123!", "Store 2"), CancellationToken.None);

        // Assert
        response1.StoreId.Should().NotBeEmpty();
        response2.StoreId.Should().NotBeEmpty();
        response1.StoreId.Should().NotBe(response2.StoreId);

        var stores = await dbContext.Stores.ToListAsync();
        stores.Should().HaveCount(2);
        stores.Select(s => s.Name).Should().Contain(new[] { "Store 1", "Store 2" });
    }

    [Fact]
    public async Task Products_ShouldBeIsolatedBetweenStores()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var storeId1 = Guid.NewGuid();
        var storeId2 = Guid.NewGuid();

        var user1Service = new TestCurrentUserService { StoreId = storeId1, UserId = Guid.NewGuid(), IsOwner = true };
        var user2Service = new TestCurrentUserService { StoreId = storeId2, UserId = Guid.NewGuid(), IsOwner = true };

        var handler1 = new ProductHandlers(dbContext, user1Service);
        var handler2 = new ProductHandlers(dbContext, user2Service);

        // Act - Store 1 creates a product with SKU "SKU-100"
        await handler1.Handle(new CreateProductCommand(null, "Store 1 Item", "SKU-100", 99.00m, 10), CancellationToken.None);

        // Act - Store 2 should be able to create a product with the SAME SKU "SKU-100" without conflict
        var store2Product = await handler2.Handle(new CreateProductCommand(null, "Store 2 Item", "SKU-100", 149.00m, 5), CancellationToken.None);

        var store1Catalog = await handler1.Handle(new GetProductsQuery(null, null), CancellationToken.None);
        var store2Catalog = await handler2.Handle(new GetProductsQuery(null, null), CancellationToken.None);

        // Assert - Each store only sees its own products
        store1Catalog.Should().HaveCount(1);
        store1Catalog[0].Name.Should().Be("Store 1 Item");

        store2Catalog.Should().HaveCount(1);
        store2Catalog[0].Name.Should().Be("Store 2 Item");
    }

    [Fact]
    public async Task UserFromStore1_CannotAccessOrDeleteStore2Product()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var storeId1 = Guid.NewGuid();
        var storeId2 = Guid.NewGuid();

        var user1Service = new TestCurrentUserService { StoreId = storeId1, UserId = Guid.NewGuid(), IsOwner = true };
        var user2Service = new TestCurrentUserService { StoreId = storeId2, UserId = Guid.NewGuid(), IsOwner = true };

        var handler1 = new ProductHandlers(dbContext, user1Service);
        var handler2 = new ProductHandlers(dbContext, user2Service);

        var product2 = await handler2.Handle(new CreateProductCommand(null, "Store 2 Exclusive", "SKU-EXCLUSIVE", 50.00m, 10), CancellationToken.None);

        // Act & Assert - Store 1 querying or deleting Store 2's product fails with NotFoundException
        var queryAct = () => handler1.Handle(new GetProductByIdQuery(product2.Id), CancellationToken.None);
        await queryAct.Should().ThrowAsync<NotFoundException>();

        var deleteAct = () => handler1.Handle(new DeleteProductCommand(product2.Id), CancellationToken.None);
        await deleteAct.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SalesAndReports_ShouldBePartitionedByStore()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var storeId1 = Guid.NewGuid();
        var storeId2 = Guid.NewGuid();

        var user1Service = new TestCurrentUserService { StoreId = storeId1, UserId = Guid.NewGuid(), IsOwner = true };
        var user2Service = new TestCurrentUserService { StoreId = storeId2, UserId = Guid.NewGuid(), IsOwner = true };

        var user1 = new User
        {
            Id = user1Service.UserId,
            Email = "user1@store1.com",
            PasswordHash = "hash",
            Role = UserRole.Owner,
            StoreId = storeId1,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user1);
        await dbContext.SaveChangesAsync();

        var productHandler1 = new ProductHandlers(dbContext, user1Service);
        var p1 = await productHandler1.Handle(new CreateProductCommand(null, "Milk 1L", "MILK-1L", 60.00m, 20), CancellationToken.None);

        var salesHandler1 = new SaleHandlers(dbContext, user1Service);
        var salesHandler2 = new SaleHandlers(dbContext, user2Service);

        // Act - Store 1 rings up a sale
        await salesHandler1.Handle(new CreateSaleCommand(PaymentMethod.Cash, new[] { new SaleLineRequest(p1.Id, 2) }), CancellationToken.None);

        // Assert - Store 1 sees 1 sale; Store 2 sees 0 sales
        var store1Sales = await salesHandler1.Handle(new GetSalesQuery(1, 25), CancellationToken.None);
        var store2Sales = await salesHandler2.Handle(new GetSalesQuery(1, 25), CancellationToken.None);

        store1Sales.TotalCount.Should().Be(1);
        store1Sales.Items[0].TotalAmount.Should().Be(120.00m);

        store2Sales.TotalCount.Should().Be(0);

        // Reports check
        var reportHandler1 = new ReportHandlers(dbContext, user1Service);
        var reportHandler2 = new ReportHandlers(dbContext, user2Service);

        var store1Revenue = await reportHandler1.Handle(new GetDailyRevenueQuery(), CancellationToken.None);
        var store2Revenue = await reportHandler2.Handle(new GetDailyRevenueQuery(), CancellationToken.None);

        store1Revenue.Should().HaveCount(1);
        store1Revenue[0].TotalRevenue.Should().Be(120.00m);

        store2Revenue.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCashier_ShouldInheritStoreIdOfAuthenticatedOwner()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var hasher = new MockPasswordHasher();
        var storeId = Guid.NewGuid();
        var ownerService = new TestCurrentUserService { StoreId = storeId, UserId = Guid.NewGuid(), IsOwner = true };

        var handler = new CreateCashierCommandHandler(dbContext, hasher, ownerService);

        // Act
        var cashierResponse = await handler.Handle(new CreateCashierCommand("cashier@store1.com", "Password123!"), CancellationToken.None);

        // Assert
        cashierResponse.StoreId.Should().Be(storeId);
        cashierResponse.Role.Should().Be(UserRole.Cashier.ToString());
    }
}
