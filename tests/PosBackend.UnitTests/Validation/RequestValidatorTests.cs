using FluentAssertions;
using PosBackend.Application.Products;
using PosBackend.Application.Sales;
using PosBackend.Domain.Enums;
using Xunit;

namespace PosBackend.UnitTests.Validation;

public class RequestValidatorTests
{
    private readonly CreateProductValidator _productValidator;
    private readonly CreateSaleValidator _saleValidator;

    public RequestValidatorTests()
    {
        _productValidator = new CreateProductValidator();
        _saleValidator = new CreateSaleValidator();
    }

    [Fact]
    public void ProductValidator_ShouldHaveError_WhenNameIsEmpty()
    {
        // Arrange
        var command = new CreateProductCommand(Guid.NewGuid(), "", "SKU123", 10.99m, 5);

        // Act
        var result = _productValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void ProductValidator_ShouldHaveError_WhenPriceIsZeroOrNegative()
    {
        // Arrange
        var command = new CreateProductCommand(Guid.NewGuid(), "Valid Name", "SKU123", 0.00m, 5);

        // Act
        var result = _productValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(CreateProductCommand.Price));
    }

    [Fact]
    public void ProductValidator_ShouldNotHaveError_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateProductCommand(Guid.NewGuid(), "Valid Name", "SKU123", 10.99m, 5);

        // Act
        var result = _productValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SaleValidator_ShouldHaveError_WhenItemsEmpty()
    {
        // Arrange
        var command = new CreateSaleCommand(PaymentMethod.Cash, new List<SaleLineRequest>());

        // Act
        var result = _saleValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(CreateSaleCommand.Items));
    }

    [Fact]
    public void SaleValidator_ShouldHaveError_WhenItemQuantityIsNegative()
    {
        // Arrange
        var items = new List<SaleLineRequest>
        {
            new SaleLineRequest(Guid.NewGuid(), -1)
        };
        var command = new CreateSaleCommand(PaymentMethod.Cash, items);

        // Act
        var result = _saleValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Quantity"));
    }
}
