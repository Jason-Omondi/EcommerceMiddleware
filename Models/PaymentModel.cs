namespace EcommerceMiddleware.Models
{
    public class PaymentModel
    {
        public class Payment
        {
            public int PaymentId { get; set; }
            public int OrderId { get; set; } // Foreign key to Order
            public string PaymentMethod { get; set; } // e.g., PayPal, Credit Card
            public decimal Amount { get; set; }
            public DateTime PaymentDate { get; set; }
            public string Status { get; set; } // e.g., Success, Failed

            // Navigation Property
            public virtual Order Order { get; set; }
        }

    }
}
