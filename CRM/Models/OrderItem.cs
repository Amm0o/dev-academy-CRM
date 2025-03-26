using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CRM.Models;


// Represents a single order's collection of products with their quantities, tied to an order ID
public class OrderItem : Entity
{
    // OrderId uses the inherited Id from Entity
    public int OrderId 
    { 
        get => Id; 
        set => Id = value; 
    }

    [Required(ErrorMessage = "At least one product is required")]
    public List<ProductQuantity> ProductQuantities { get; set; } = new List<ProductQuantity>();

    // Constructor with initial products
    public OrderItem(int orderId, List<ProductQuantity> productQuantities)
    {
        if (orderId <= 0)
            throw new ArgumentException("Order ID must be a positive number", nameof(orderId));
        if (productQuantities == null || productQuantities.Count == 0)
            throw new ArgumentException("At least one product quantity must be specified", nameof(productQuantities));

        OrderId = orderId;
        ProductQuantities = productQuantities;
    }

    // Parameterless constructor for EF/serialization
    public OrderItem() { }

    // Helper method to add products
    public void AddProduct(int productId, int quantity)
    {
        ProductQuantities.Add(new ProductQuantity(productId, quantity));
    }


    // Helper class to store product-quantity pairs
    public class ProductQuantity
    {
        [Required(ErrorMessage = "Product ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Product ID must be a positive number")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "At least one product quantity must be specified")]
        public int Quantity { get; set; }

        public ProductQuantity(int productId, int quantity)
        {
            if (productId <= 0)
                throw new ArgumentException("Product ID must be a positive number", nameof(productId));
            if (quantity <= 0)
                throw new ArgumentException("At least one product quantity must be specified", nameof(quantity));

            ProductId = productId;
            Quantity = quantity;
        }

        // Parameterless constructor for EF/serialization
        public ProductQuantity() { }
    }
}

