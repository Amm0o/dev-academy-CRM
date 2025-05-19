
namespace CRM.Models
{
    public class Cart : Entity
    {

        public int UserId { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        public Cart(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be a positive Integer", nameof(userId));
            UserId = userId;
        }

        public void AddItem(OrderItem item)
        {
            Items.Add(item);
        }

        public void RemoveItem(int productId)
        {
            Items.RemoveAll(i => i.ProductId == productId);
        }

        public void Clear()
        {
            Items.Clear();
        }

        
        
    }
}