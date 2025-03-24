using System.ComponentModel.DataAnnotations;

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
    public required float ProductPrice {get; set;}

    [Required(ErrorMessage = "The product needs to have a quantity that's >= 0")]
    [Range(0, int.MaxValue, ErrorMessage = "The quantity of the product needs to be >= 0")]
    public required int ProductQuantity {get; set;}

    [Required(ErrorMessage = "Product needs to have a category at least N/A")]
    [StringLength(50, MinimumLength = 3)]
    public required string ProdcutCategory;

}