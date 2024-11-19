namespace EcommerceMiddleware.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; } // Foreign key to Order
        public string ProductId { get; set; } // Foreign key to Product
        public int Quantity { get; set; }
        public decimal Price { get; set; } // Price at the time of order

        // Navigation Properties
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }

       // public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}
