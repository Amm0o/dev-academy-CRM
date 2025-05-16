using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CRM.Models;

public sealed class Order : Entity
{
    // Base order properties
    [Required(ErrorMessage = "An order needs to be associated with a customer")]
    public int CustomerId { get; set; }
    
    [Required(ErrorMessage = "Username of the person ordering is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "The UserNameOrder must be at least 3 chars")]
    public required string UserNameOrder { get; set; }

    [StringLength(500)]
    public string OrderDescription { get; set; } = string.Empty;

    [Required]
    public DateTime OrderDate { get; private set; } = DateTime.UtcNow;
    
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    // External reference ID (GUID)
    [Required(ErrorMessage = "An order needs a valid GUID")]
    public Guid OrderGuid { get; private set; } = Guid.NewGuid();

    // Order items collection
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Calculated properties
    [Required]
    public decimal TotalAmount => _items.Sum(item => item.LineTotal);

    // Constructors
    public Order() { } // For ORM/serialization
    
    [SetsRequiredMembers]
    public Order(string userNameOrder, int customerId, string orderDescription = "", Guid? orderGuid = null)
    {
        if (string.IsNullOrWhiteSpace(userNameOrder) || userNameOrder.Length < 3)
        {
            throw new ArgumentException("The UserNameOrder must be at least 3 characters long.", nameof(userNameOrder));
        }

        if (customerId <= 0)
        {
            throw new ArgumentException("Customer ID must be a positive number", nameof(customerId));
        }

        UserNameOrder = userNameOrder;
        CustomerId = customerId;
        OrderDescription = orderDescription ?? "No description provided";
        
        // Use provided GUID or keep the default one
        if (orderGuid.HasValue && orderGuid.Value != Guid.Empty)
        {
            OrderGuid = orderGuid.Value;
        }
    }

    // Methods to manage items
    public void AddItem(int productId, int quantity, decimal unitPrice)
    {
        _items.Add(new OrderItem(0, productId, quantity, unitPrice));
    }

    public void AddItem(OrderItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        _items.Add(item);
    }

    public bool RemoveItem(int productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            return _items.Remove(item);
        }
        return false;
    }

    public void ClearItems()
    {
        _items.Clear();
    }
    
    // Helper methods for order processing
    public void MarkAsProcessed()
    {
        Status = "Processed";
    }
    
    public void MarkAsShipped()
    {
        Status = "Shipped";
    }
    
    public void MarkAsDelivered()
    {
        Status = "Delivered";
    }
    
    public void MarkAsCancelled()
    {
        Status = "Cancelled";
    }
}