using System;
using System.Collections.Generic;
using System.Linq;

namespace CRM.Models
{
    public class Carts : Entity
    {


        public int CartId { get; private set; }
        public int UserId { get; private set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Collection of Cart Items
        public List<CartItems> Items { get; set; } = new List<CartItems>();

        public Carts() { }

        // Constructor for creating a new Cart
        public Carts(int userId)
        {
            if (userId < 0)
                throw new ArgumentException("UserId must be positive integer", nameof(userId));

            UserId = userId;
        }


        // Helper methods for Cart operations
        public void AddItem(int productId, int quantity, decimal unitPrice)
        {
            // Check if Item already exists
            var existingItem = Items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.UpdateQuantity(existingItem.Quantity + quantity);
            }
            else
            {
                Items.Add(new CartItems(CartId, productId, quantity, unitPrice));
            }

            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveItem(int productId)
        {
            Items.RemoveAll(i => i.ProductId == productId);
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateItemQuantity(int productId, int productQuantity)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                item.UpdateQuantity(productQuantity);
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void Clear()
        {
            Items.Clear();
            UpdatedAt = DateTime.UtcNow;
        }

        // Calculate total cart value
        public decimal TotalCartValue => Items.Sum(i => i.ItemTotal);


    }
}