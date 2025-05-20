namespace CRM.Models
{
    public class CartItems
    {
        public int CartItemId { get; private set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public CartItems() { }

        // Constructor for creating new CartItems

        public CartItems(int cartId, int productId, int quantity, decimal unitPrice)
        {
            if (cartId <= 0)
                throw new ArgumentException("CartId must be positive", nameof(cartId));

            if (productId <= 0)
                throw new ArgumentException("Product Id must be positive", nameof(productId));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));

            if (unitPrice < 0)
                throw new ArgumentException("UnitPrice cannot be negative", nameof(unitPrice));

            CartId = cartId;
            ProductId = productId;
            Quantity = quantity;
            UnitPrice = unitPrice;

        }

        // Calculated Property
        public decimal ItemTotal => Quantity * UnitPrice;

        // Method to update quantity
        public void UpdateQuantity(int newQuantity)
        {
            if (newQuantity < 0)
            {
                throw new ArgumentException("Quantity must be positive", nameof(newQuantity));
            }

            Quantity = newQuantity;
        }
    }
}