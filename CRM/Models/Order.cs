using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CRM.Models;

public sealed class Order : Entity
{
    [Required(ErrorMessage = "An order needs to be associated with a user that ordered")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "The UserNameOrder must be at 3 chars")]
    public required string UserNameOrder { get; set; }

    [StringLength(500)]
    public string OrderDescription { get; set; }

    [Required(ErrorMessage = "A product needs a quantity that's >= 0")]
    [Range(0, int.MaxValue, ErrorMessage = "The product quantity needs to be >= 0 and < int64")]
    public required int StockQuantity { get; set; }

    [Required(ErrorMessage = "A product needs a category")]
    [StringLength(100, MinimumLength = 3)]
    public required string ProductCategory { get; set; }

    public DateTime OrderDate { get; private set; }

    [SetsRequiredMembers]
    public Order(string userNameOrder, string orderDescription, int stockQuantity, string productCategory)
    {
        if (string.IsNullOrWhiteSpace(userNameOrder) || userNameOrder.Length < 3)
        {
            throw new ArgumentException("The UserNameOrder must be at least 3 characters long.", nameof(userNameOrder));
        }

        if (stockQuantity < 0)
        {
            throw new ArgumentException("StockQuantity must be greater than or equal to 0.", nameof(stockQuantity));
        }

        if (string.IsNullOrWhiteSpace(productCategory) || productCategory.Length < 3)
        {
            throw new ArgumentException("The ProductCategory must be at least 3 characters long.", nameof(productCategory));
        }

        UserNameOrder = userNameOrder;
        OrderDescription = string.IsNullOrWhiteSpace(orderDescription) 
            ? "There was no description provided" 
            : orderDescription;
        StockQuantity = stockQuantity;
        ProductCategory = productCategory;
        OrderDate = DateTime.UtcNow;
    }
}