using EcommerceMiddleware.Models;

public class Order
{
    //public virtual User User { get; set; } // Foreign Key relationship
    public DateTime CreatedAt { get; set; }
    public string OrderStatus { get; set; }

    public int OrderId { get; set; }
    public string UserId { get; set; } // Foreign key to User
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } // e.g., Paid, Pending, Failed

    // Navigation Property
    public virtual User User { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; }



}
