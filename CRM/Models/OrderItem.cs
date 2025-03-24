using System.ComponentModel.DataAnnotations;

namespace CRM.Models;


// OrderId, ProductId, Quantity
class OrderItem : Entity {

    // OrderId is reference to the inherited Id from Entity
    public int OrderId {
        get => Id; // return Inherited Id 
        set => Id = value; // Set inheritedId
    }

    [Required(ErrorMessage = "An order needs to have a ProductId")]
    [Range(0, int.MaxValue, ErrorMessage = "The product Id needs to be >= 0 up to int.Max")]
    public required int OrderProductId;

    [Required(ErrorMessage = "An order needs a quantity that's greater than 0")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity cannot be greater than int64")]
    public required int OrderQuantity;


}