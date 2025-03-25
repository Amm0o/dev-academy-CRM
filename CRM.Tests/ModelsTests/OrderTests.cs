using CRM.Models;
using Xunit;

namespace CRM.Tests.ModelsTests;

public class OrderTests
{

    string userNameOrder = "Jane Doe";
    string orderDescription = "test description";
    int stockQuantity = 2;
    string productCategory = "Test Category";
    [Fact]
    public void Order_Constructor_ValidData_InitializesCorrectly()
    {
        var order = new Order(userNameOrder, orderDescription, stockQuantity, productCategory);

        Assert.Equal(userNameOrder, order.UserNameOrder);
        Assert.Equal(orderDescription, order.OrderDescription);
        Assert.Equal(stockQuantity, order.StockQuantity);
        Assert.Equal(productCategory, order.ProductCategory);
        Assert.True(order.OrderDate <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_InvalidUserNameOrder_ThrowsArgumentException()
    {
        // Arrange
        string invalidUserNameOrder = "Jo"; // Less than 3 characters

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Order(invalidUserNameOrder, orderDescription, stockQuantity, productCategory));
    }

    [Fact]
    public void Constructor_InvalidStockQuantity_ThrowsArgumentException() {
        int invalidSockQuantity = -1;

        Assert.Throws<ArgumentException>(() => 
            new Order(userNameOrder, orderDescription, invalidSockQuantity, productCategory));
    }

    [Fact]
    public void Constructor_InvalidProductCategory_ThrowsArgumentException() {
        string invalidProductCategory = "NA";

        Assert.Throws<ArgumentException>(() => 
            new Order(userNameOrder, orderDescription, stockQuantity, invalidProductCategory));
    }
}