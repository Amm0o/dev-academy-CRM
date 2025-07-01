using Xunit;
using CRM.Models;
using System.Collections.Generic;
using System.Linq;

namespace CRM.Tests.ModelsTests
{
    public class CartTests
    {
        [Fact]
        public void Cart_ShouldInitializeWithEmptyItems()
        {
            // Arrange & Act
            var cart = new Carts();

            // Assert
            Assert.NotNull(cart.Items);
            Assert.Empty(cart.Items);
        }

        [Fact]
        public void CartItem_ShouldCalculateTotalPrice()
        {
            // Arrange
            var cartItem = new CartItems
            {
                ProductId = 1,
                ProductName = "Test Product",
                UnitPrice = 10.50m,
                Quantity = 3
            };

            // Act
            var total = cartItem.ItemTotal;

            // Assert
            Assert.Equal(31.50m, total);
        }

        [Fact]
        public void Cart_ShouldCalculateTotalAmount()
        {
            // Arrange
            var cart = new Carts
            {
                Items = new List<CartItems>
                {
                    new CartItems { UnitPrice = 10.00m, Quantity = 2 },
                    new CartItems { UnitPrice = 15.50m, Quantity = 1 },
                    new CartItems { UnitPrice = 5.25m, Quantity = 4 }
                }
            };

            // Act
            var total = cart.TotalCartValue;

            // Assert
            Assert.Equal(56.50m, total);
        }

    }
}