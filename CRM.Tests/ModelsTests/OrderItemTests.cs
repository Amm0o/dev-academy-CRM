using CRM.Models;
using Xunit;
using System;

namespace CRM.Tests.ModelsTests;

public class OrderItemsTests
{
    // Test data
    private const int ValidOrderId = 1;
    private const int ValidProductId = 12314;
    private const int ValidQuantity = 20;
    private const decimal ValidUnitPrice = 10.50m;

    [Fact]
    public void Constructor_ValidData_InitializesCorrectly()
    {
        // Based on actual OrderItem constructor
        var orderItem = new OrderItem(ValidOrderId, ValidProductId, ValidQuantity, ValidUnitPrice);

        Assert.Equal(ValidOrderId, orderItem.OrderId);
        Assert.Equal(ValidProductId, orderItem.ProductId);
        Assert.Equal(ValidQuantity, orderItem.Quantity);
        Assert.Equal(ValidUnitPrice, orderItem.UnitPrice);
        Assert.Equal(ValidQuantity * ValidUnitPrice, orderItem.LineTotal);
    }

    [Fact]
    public void Constructor_NegativeOrderId_CreatesWithNegativeValue()
    {
        // The constructor doesn't validate, so it will create with negative value
        var orderItem = new OrderItem(-1, ValidProductId, ValidQuantity, ValidUnitPrice);
        
        Assert.Equal(-1, orderItem.OrderId);
    }

    [Fact]
    public void ParameterlessConstructor_CreatesDefaultValues()
    {
        // Act
        var orderItem = new OrderItem();

        // Assert
        Assert.Equal(0, orderItem.OrderId);
        Assert.Equal(0, orderItem.ProductId);
        Assert.Equal(0, orderItem.Quantity);
        Assert.Equal(0m, orderItem.UnitPrice);
    }

    [Fact]
    public void Quantity_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var orderItem = new OrderItem(ValidOrderId, ValidProductId, ValidQuantity, ValidUnitPrice);
        int newQuantity = 30;

        // Act
        orderItem.Quantity = newQuantity;

        // Assert
        Assert.Equal(newQuantity, orderItem.Quantity);
        Assert.Equal(newQuantity * ValidUnitPrice, orderItem.LineTotal);
    }

    [Fact]
    public void Quantity_SetNegativeValue_ShouldBeAllowedOrNot()
    {
        // Arrange
        var orderItem = new OrderItem(ValidOrderId, ValidProductId, ValidQuantity, ValidUnitPrice);

        // Act - Try to set negative quantity
        // Note: This depends on whether the setter has validation
        // If it doesn't throw, we'll just verify the value is set
        orderItem.Quantity = -1;
        
        // Assert - If no exception is thrown, verify the value
        Assert.Equal(-1, orderItem.Quantity);
    }

    [Fact]
    public void LineTotal_CalculatesCorrectly()
    {
        // Arrange
        var orderItem = new OrderItem(ValidOrderId, ValidProductId, 5, 10.00m);

        // Assert
        Assert.Equal(50.00m, orderItem.LineTotal);
    }

    [Fact]
    public void OrderId_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var orderItem = new OrderItem();

        // Act
        orderItem.OrderId = ValidOrderId;

        // Assert
        Assert.Equal(ValidOrderId, orderItem.OrderId);
    }

    [Fact]
    public void LineTotal_UpdatesWhenQuantityChanges()
    {
        // Arrange
        var orderItem = new OrderItem(ValidOrderId, ValidProductId, 5, 10.00m);
        
        // Act
        orderItem.Quantity = 10;
        
        // Assert
        Assert.Equal(100.00m, orderItem.LineTotal);
    }

    [Fact]
    public void LineTotal_UpdatesWhenUnitPriceChanges()
    {
        // Arrange
        var orderItem = new OrderItem(ValidOrderId, ValidProductId, 5, 10.00m);
        
        // Act
        orderItem.UnitPrice = 20.00m;
        
        // Assert
        Assert.Equal(100.00m, orderItem.LineTotal);
    }
}