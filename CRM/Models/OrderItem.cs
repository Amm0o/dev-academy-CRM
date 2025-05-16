using System.ComponentModel.DataAnnotations;

namespace CRM.Models;

public class OrderItem : Entity
{
    [Required]
    public int OrderId { get; set; }
    
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than zero")]
    public decimal UnitPrice { get; set; }
    
    // Navigation properties (if using EF Core)
    // public Order Order { get; set; }
    // public Product Product { get; set; }
    
    // Calculate line total
    public decimal LineTotal => Quantity * UnitPrice;
    
    public OrderItem(int orderId, int productId, int quantity, decimal unitPrice)
    {
        if (orderId <= 0) throw new ArgumentException("Order ID must be positive", nameof(orderId));
        if (productId <= 0) throw new ArgumentException("Product ID must be positive", nameof(productId));
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (unitPrice <= 0) throw new ArgumentException("Unit price must be positive", nameof(unitPrice));
        
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
    
    // Parameter-less constructor for serialization/EF
    public OrderItem() { }
}