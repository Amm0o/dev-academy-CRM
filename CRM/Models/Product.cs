using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CRM.Models;

// Rethink this Entity inheritence. 
public sealed class Product : Entity 
{

    [Required(ErrorMessage = "Name of the product is required")]
    [StringLength(100, MinimumLength = 2)]
    public required string ProductName { get; set; }

    [Required(ErrorMessage = "A product needs a description")]
    [StringLength(300)]
    public required string ProductDescription {get; set;}

    [Required(ErrorMessage = "A product needs a prices!")]
    [Range(0, double.MaxValue, ErrorMessage = "Price must >= 0 and < double Max")]
    public required double ProductPrice {get; set;}

    [Required(ErrorMessage = "The product needs to have a quantity that's >= 0")]
    [Range(0, int.MaxValue, ErrorMessage = "The quantity of the product needs to be >= 0")]
    public required int ProductQuantity {get; set;}

    [Required(ErrorMessage = "Product needs to have a category at least N/A")]
    [StringLength(50, MinimumLength = 3)]
    public required string ProdcutCategory;

    private DateTime ProductUpdateTime;

    [SetsRequiredMembers]
    public Product (string productName, string productDescription, double productPrice, int productQuantity, string productCategory){ 


        // Checks

        if (string.IsNullOrWhiteSpace(productName) || productName.Length < 2) {
            throw new ArgumentException("Name of product is required and needs to be at least 2");
        }

        if(string.IsNullOrWhiteSpace(productDescription)) {
            throw new ArgumentException("A product needs a description");
        }

        if (productPrice < 0 || productPrice > double.MaxValue) {
            throw new ArgumentException("Product price must be between 0 and the maximum value for a double.");
        }

        if (productQuantity < 0 || productQuantity > int.MaxValue) {
            throw new ArgumentException("The product needs to have a quantity that's >= 0");
        }

        if (string.IsNullOrWhiteSpace(productCategory) || productCategory.Length < 3) {
            throw new ArgumentException("Product needs to have a category at least N/A");
        }

        // Set values
        ProductName = productName;
        ProductDescription = productDescription;
        ProdcutCategory = productCategory;
        ProductUpdateTime = DateTime.UtcNow;
        ProductPrice = productPrice;
        ProductQuantity = productQuantity;

    }

}