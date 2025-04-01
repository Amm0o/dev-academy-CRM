using CRM.Models;
using Xunit;
using System;
using System.Collections.Generic;

namespace CRM.Tests.ModelsTests;

public class OrderItemsTests
{
    // Test data
    private const int ValidOrderId = 1;
    private const int ValidProductId = 12314;
    private const int ValidQuantity = 20;

    [Fact]
    public void Constructor_ValidData_InitializesCorrectly()
    {
        // Arrange
        var initialProductQuantities = new List<OrderItem.ProductQuantity>
        {
            new OrderItem.ProductQuantity(ValidProductId, ValidQuantity) // Ensure at least one valid product-quantity pair
        };
        var orderItem = new OrderItem(ValidOrderId, initialProductQuantities);
        int newProductId = 45678;
        int newQuantity = 10;

        // Act
        orderItem.AddProduct(newProductId, newQuantity);

        // Assert
        Assert.Equal(2, orderItem.ProductQuantities.Count); // Ensures two items
        Assert.Equal(newProductId, orderItem.ProductQuantities[1].ProductId);
        Assert.Equal(newQuantity, orderItem.ProductQuantities[1].Quantity);
    }

    [Fact]
    public void Constructor_NegativeOrderId_ThrowsArgumentException()
    {
        // Arrange
        var productQuantities = new List<OrderItem.ProductQuantity>
        {
            new OrderItem.ProductQuantity(ValidProductId, ValidQuantity)
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new OrderItem(-1, productQuantities));
        Assert.Equal("Order ID must be a positive number (Parameter 'orderId')", exception.Message);
        Assert.Equal("orderId", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullProductQuantities_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new OrderItem(ValidOrderId, null));
        Assert.Equal("Quantity must be a positive number > 0 (Parameter 'productQuantities')", exception.Message);
        Assert.Equal("productQuantities", exception.ParamName);
    }

    [Fact]
    public void Constructor_EmptyProductQuantities_ThrowsArgumentException()
    {
        // Arrange
        var emptyProductQuantities = new List<OrderItem.ProductQuantity>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new OrderItem(ValidOrderId, emptyProductQuantities));
        Assert.Equal("Quantity must be a positive number > 0 (Parameter 'productQuantities')", exception.Message);
        Assert.Equal("productQuantities", exception.ParamName);
    }

    [Fact]
    public void ParameterlessConstructor_CreatesEmptyProductList()
    {
        // Act
        var orderItem = new OrderItem();

        // Assert
        Assert.Equal(0, orderItem.OrderId); // Default value from Entity
        Assert.NotNull(orderItem.ProductQuantities);
        Assert.Empty(orderItem.ProductQuantities);
    }

    [Fact]
    public void AddProduct_ValidData_AddsToList()
    {
        // Arrange
        var initialProductQuantities = new List<OrderItem.ProductQuantity>
        {
            new OrderItem.ProductQuantity(ValidProductId, ValidQuantity)
        };
        var orderItem = new OrderItem(ValidOrderId, initialProductQuantities);
        int newProductId = 45678;
        int newQuantity = 10;

        // Act
        orderItem.AddProduct(newProductId, newQuantity);

        // Assert
        Assert.Equal(2, orderItem.ProductQuantities.Count); // Now expecting 2 items
        Assert.Equal(newProductId, orderItem.ProductQuantities[1].ProductId);
        Assert.Equal(newQuantity, orderItem.ProductQuantities[1].Quantity);
    }

    [Fact]
    public void ProductQuantityConstructor_NegativeProductId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new OrderItem.ProductQuantity(-1, ValidQuantity));
        Assert.Equal("Product ID must be a positive number (Parameter 'productId')", exception.Message);
        Assert.Equal("productId", exception.ParamName);
    }

    [Fact]
    public void ProductQuantityConstructor_NegativeQuantity_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new OrderItem.ProductQuantity(ValidProductId, -1));
        Assert.Equal("Quantity must be a positive number > 0 (Parameter 'quantity')", exception.Message);
        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void OrderId_SetAndGet_ReflectsBaseId()
    {
        // Arrange
        var orderItem = new OrderItem();

        // Act
        orderItem.OrderId = ValidOrderId;

        // Assert
        Assert.Equal(ValidOrderId, orderItem.OrderId);
        Assert.Equal(ValidOrderId, orderItem.Id); // Inherited from Entity
    }
}