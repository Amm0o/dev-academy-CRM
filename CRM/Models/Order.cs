using System.ComponentModel.DataAnnotations;

namespace CRM.Models;

// Product: Id, Name, Description, Price, StockQuantity, Category.

class Order : Entity {

    [Required(ErrorMessage = "An order needs to be associated with a user that ordered")]
    [StringLength(100, MinimumLength = 3)] // Minimum is N/A
    public required string UserOrder {get; set;}

    [StringLength(500)]
    public string OrderDescription {get; set;}

    [Required(ErrorMessage = "A product needs a quantity that's >= 0")]
    [Range(0, int.MaxValue, ErrorMessage = "The product quantity needs to be >= 0 and < int64")]
    public required int StockQuantity;

    [Required(ErrorMessage = "A product needs a category")]
    public required string ProdcutCategory;


    // Constructor to set the default values
    public Order() {
        OrderDescription = "There was no description provided";
    }
}