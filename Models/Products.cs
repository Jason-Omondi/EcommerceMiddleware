//public class Product
//{
//    public int ProductId { get; set; }
//    public string Name { get; set; }
//    public string Description { get; set; }
//    public decimal Price { get; set; }
//    public string ImageUrl { get; set; }
//    public int AvailableQuantity { get; set; }
//    public string DataSource { get; set; } // Amazon/eBay
//}


public class Product
{
    public int ProductId { get; set; }    // Maps to `id` in FakeStore
    public string Name { get; set; }       // Maps to `title` in FakeStore
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public string ImageUrl { get; set; }   // Maps to `image` in FakeStore
                                           // public Rating Rating { get; set; }     // Average rating
    public double Rating { get; set; }     // Average rating
    public int RatingCount { get; set; }   // Number of ratings
    public string DataSource { get; set; } // Should remain as vendor identifier
}

//public class Rating
//{
//    public double rate { get; set; }
//    public int count { get; set; }
//}

