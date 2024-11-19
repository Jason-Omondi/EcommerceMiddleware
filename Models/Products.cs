
//ebay + fakestore model intergration
using System.ComponentModel.DataAnnotations.Schema;

public class Product
{
    public string ProductId { get; set; }      // Maps to `epid` in eBay and `id` in FakeStore
    public string Name { get; set; }           // Maps to `title`
    public string Description { get; set; }    // For FakeStore, use `description`; for eBay, can be optional
    public string Category { get; set; }       // General category or condition type if applicable
    public string ImageUrl { get; set; }       // Maps to `image` in FakeStore and `image.imageUrl` in eBay
    public double Rating { get; set; }         // Maps to `averageRating`
    public int RatingCount { get; set; }       // Maps to `ratingCount` or `reviewCount`
    public string DataSource { get; set; }     // "FakeStore" or "eBay"

    public decimal Price { get; set; } //"Fake store" only, for ebay it is through classes below

    // Holds multiple conditions with associated prices, used for eBay data
    [NotMapped]
    public List<ConditionPrice> ConditionPrices { get; set; }

    // Holds rating aspects like "Good value," "Long battery life"
    [NotMapped]
    public List<RatingAspect> RatingAspects { get; set; }

    // Holds additional vendor-specific fields
    [NotMapped]
    public SourceData VendorSpecificData { get; set; }

    // Price and additional fields
    [NotMapped]
    public decimal DiscountPercentage { get; set; } // Maps to `discountPercentage`

    [NotMapped]
    public int Stock { get; set; }                 // Maps to `stock`

    [NotMapped]
    public List<string> Tags { get; set; }         // Maps to `tags` array

    [NotMapped]
    public string Brand { get; set; }              // Maps to `brand`


    [NotMapped]
    public string SKU { get; set; }                // Maps to `sku`

    [NotMapped]
    public decimal Weight { get; set; }            // Maps to `weight`

    [NotMapped]
    public Dimensions Dimensions { get; set; }     // Maps to `dimensions` object


    [NotMapped]
    public string WarrantyInformation { get; set; } // Maps to `warrantyInformation`


    [NotMapped]
    public string ShippingInformation { get; set; } // Maps to `shippingInformation`

    [NotMapped]
    public string AvailabilityStatus { get; set; } // Maps to `availabilityStatus`

    [NotMapped]
    public List<Review> Reviews { get; set; }      // Maps to `reviews` array

    [NotMapped]
    public string ReturnPolicy { get; set; }       // Maps to `returnPolicy`

    [NotMapped]
    public int MinimumOrderQuantity { get; set; }  // Maps to `minimumOrderQuantity`


    [NotMapped]
    public MetaData Meta { get; set; }             // Maps to `meta` object

    [NotMapped]
    public List<string> Images { get; set; }       // Maps to `images` array


}

public class MetaData
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Barcode { get; set; }
    public string QrCode { get; set; }
}

public class Review
{
    public int Rating { get; set; }
    public string Comment { get; set; }
    public DateTime Date { get; set; }
    public string ReviewerName { get; set; }
    public string ReviewerEmail { get; set; }
}

public class Dimensions
{
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal Depth { get; set; }
}

public class ProductResponse
{
    public List<Product> MerchandisedProducts { get; set; }
    public RequestMetadata Metadata { get; set; }
}


public class ConditionPrice
{
    public string ConditionGroup { get; set; }  // e.g., "NEW_OTHER," "USED," "REFURBISHED," etc.
    public decimal Price { get; set; }          // Price in KES
    public string Currency { get; set; }        // Currency (e.g., "USD") for reference
}

public class RatingAspect
{
    public string Name { get; set; }            // Name of the aspect (e.g., "Good value")
    public string Description { get; set; }     // Description of the aspect
    public int Count { get; set; }              // Total votes on this aspect
    public List<RatingAspectDistribution> Distributions { get; set; }
}

public class RatingAspectDistribution
{
    public string Value { get; set; }           // e.g., "TRUE" or "FALSE"
    public int Count { get; set; }              // Number of votes for this value
    public double Percentage { get; set; }      // Percentage of votes for this value
}

public class SourceData
{
    public string Vendor { get; set; }          // Vendor name, e.g., "eBay," "FakeStore" "Dummy json"
    public Dictionary<string, string> ExtraFields { get; set; } // Additional fields specific to each vendor
}

public class RequestMetadata
{
    public string TraceId { get; set; }
    public string EbayRequestId { get; set; }
    public string RlogId { get; set; }
    public DateTime RequestDate { get; set; }
}





//fake store
/*
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
*/


