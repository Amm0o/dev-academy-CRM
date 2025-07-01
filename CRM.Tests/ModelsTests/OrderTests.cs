using CRM.Models;
using Xunit;

namespace CRM.Tests.ModelsTests;

public class OrderTests
{
    string userNameOrder = "Jane Doe";
    int customerId = 1;
    string orderDescription = "test description";

    [Fact]
    public void Order_Constructor_ValidData_InitializesCorrectly()
    {
        // Based on the Order constructor: Order(string userNameOrder, int customerId, string orderDescription = "", Guid? orderGuid = null)
        var order = new Order(userNameOrder, customerId, orderDescription);

        Assert.Equal(userNameOrder, order.UserNameOrder);
        Assert.Equal(orderDescription, order.OrderDescription);
        Assert.Equal(customerId, order.CustomerId);
        Assert.True(order.OrderDate <= DateTime.UtcNow);
        Assert.Equal(0m, order.TotalAmount); // Should be 0 initially as no items added
    }

    [Fact]
    public void Constructor_InvalidUserNameOrder_ThrowsArgumentException()
    {
        // Arrange
        string invalidUserNameOrder = "Jo"; // Less than 3 characters

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Order(invalidUserNameOrder, customerId, orderDescription));
    }

    [Fact]
    public void Constructor_InvalidCustomerId_ThrowsArgumentException() {
        int invalidCustomerId = -1;

        Assert.Throws<ArgumentException>(() => 
            new Order(userNameOrder, invalidCustomerId, orderDescription));
    }

    [Fact]
    public void AddItem_ValidData_UpdatesTotalAmount() {
        var order = new Order(userNameOrder, customerId, orderDescription);
        
        // Add an item
        order.AddItem(1, 2, 10.50m);
        
        Assert.Equal(21.00m, order.TotalAmount);
        Assert.Single(order.Items);
    }

    [Fact]
    public void RemoveItem_ExistingProduct_RemovesSuccessfully() {
        var order = new Order(userNameOrder, customerId, orderDescription);
        order.AddItem(1, 2, 10.50m);
        
        bool result = order.RemoveItem(1);
        
        Assert.True(result);
        Assert.Empty(order.Items);
        Assert.Equal(0m, order.TotalAmount);
    }
}