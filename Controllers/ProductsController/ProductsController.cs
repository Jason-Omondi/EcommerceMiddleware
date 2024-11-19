

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using EcommerceMiddleware.Config;



namespace EcommerceMiddleware.Controllers.ProductsController
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly EcommerceDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;

        public ProductsController(EcommerceDbContext context, IDistributedCache cache, IConfiguration configuration)
        {
            _context = context;
            _cache = cache;
            _configuration = configuration;
        }


        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<DefaultConfigs.DefaultResponse>> GetProductData()
        {
            try
            {
                var products = await GetCachedProducts("vendor_products", FetchAllProductsFromVendorApi);

                return Ok(new DefaultConfigs.DefaultResponse(
                    status: DefaultConfigs.STATUS_SUCCESS,
                    message: "Products retrieved successfully",
                    return_token: HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", ""),
                    data: products,
                    res: true
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new DefaultConfigs.DefaultResponse(
                    status: DefaultConfigs.STATUS_FAIL,
                    message: "Error retrieving products: " + ex.Message,
                    return_token: HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", ""),
                    data: null,
                    res: false
                ));
            }
        }



        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var products = await GetCachedProducts("vendor_products", FetchAllProductsFromVendorApi);

            var product = products.FirstOrDefault(p => int.Parse(p.ProductId) == id);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        private async Task<List<Product>> GetCachedProducts(string cacheKey, Func<Task<List<Product>>> fetchProductsFunc)
        {
            string cachedProducts = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedProducts))
            {
                return JsonSerializer.Deserialize<List<Product>>(cachedProducts);
            }

            var products = await fetchProductsFunc();

            var serializedProducts = JsonSerializer.Serialize(products);
            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

            await _cache.SetStringAsync(cacheKey, serializedProducts, cacheOptions);

            return products;
        }

        private async Task<List<Product>> FetchAllProductsFromVendorApi()
        {
            var fakeStoreProducts = await FetchProductsFromFakeStoreVendorApi();
            var dummyJsonProducts = await FetchProductsFromDummyJsonApi();
            //var ebayResponse = await FetchProductsFromEbay();

            return fakeStoreProducts
                .Concat(dummyJsonProducts).
                ToList();
               // Concat(ebayResponse.MerchandisedProducts).ToList();
        }

        private async Task<List<Product>> FetchProductsFromFakeStoreVendorApi()
        {
            using var httpClient = new HttpClient();

            var apiUrl = _configuration["ExternalApis:FakeStoreApiUrl"];
            var usdToKesRate = decimal.Parse(_configuration["CurrencySettings:UsdToKesRate"]);
          
            var response = await httpClient.GetStringAsync(apiUrl);

            var productsFromApi = JsonSerializer.Deserialize<List<dynamic>>(response);

            return productsFromApi.Select(apiProduct => new Product
            {
                ProductId = apiProduct.GetProperty("id").GetInt32().ToString(),
                Name = apiProduct.GetProperty("title").GetString(),
                Description = apiProduct.GetProperty("description").GetString(),
                Price = apiProduct.GetProperty("price").GetDecimal() * usdToKesRate,
                Category = apiProduct.GetProperty("category").GetString(),
                ImageUrl = apiProduct.GetProperty("image").GetString(),
                Rating = apiProduct.GetProperty("rating").GetProperty("rate").GetDouble(),
                RatingCount = apiProduct.GetProperty("rating").GetProperty("count").GetInt32(),
                DataSource = "FakeStore"
            }).ToList();
        }

        private async Task<List<Product>> FetchProductsFromDummyJsonApi()
        {
            using var httpClient = new HttpClient();
            var apiUrl = _configuration["ExternalApis:DummyJsonApiUrl"];
            var usdToKesRate = decimal.Parse(_configuration["CurrencySettings:UsdToKesRate"]);

            var response = await httpClient.GetStringAsync(apiUrl);
            var document = JsonDocument.Parse(response);
            var productsFromApi = document.RootElement.GetProperty("products").EnumerateArray();

            var products = new List<Product>();

            foreach (var apiProduct in productsFromApi)
            {
                var tags = new List<string>();
                if (apiProduct.TryGetProperty("tags", out var tagsElement))
                {
                    foreach (var tag in tagsElement.EnumerateArray())
                    {
                        tags.Add(tag.GetString());
                    }
                }

                var images = new List<string>();
                if (apiProduct.TryGetProperty("images", out var imagesElement))
                {
                    foreach (var image in imagesElement.EnumerateArray())
                    {
                        images.Add(image.GetString());
                    }
                }

                var reviews = new List<Review>();
                if (apiProduct.TryGetProperty("reviews", out var reviewsElement))
                {
                    foreach (var review in reviewsElement.EnumerateArray())
                    {
                        reviews.Add(new Review
                        {
                            Rating = review.TryGetProperty("rating", out var reviewRatingElement) ? reviewRatingElement.GetInt32() : 0,
                            Comment = review.TryGetProperty("comment", out var reviewCommentElement) ? reviewCommentElement.GetString() : string.Empty,
                            Date = review.TryGetProperty("date", out var reviewDateElement) ? DateTime.Parse(reviewDateElement.GetString()) : default,
                            ReviewerName = review.TryGetProperty("reviewerName", out var reviewNameElement) ? reviewNameElement.GetString() : string.Empty,
                            ReviewerEmail = review.TryGetProperty("reviewerEmail", out var reviewEmailElement) ? reviewEmailElement.GetString() : string.Empty
                        });
                    }
                }

                var dimensions = new Dimensions
                {
                    Width = apiProduct.TryGetProperty("dimensions", out var dimensionsElement) && dimensionsElement.TryGetProperty("width", out var dimWidthElement) ? dimWidthElement.GetDecimal() : 0,
                    Height = apiProduct.TryGetProperty("dimensions", out dimensionsElement) && dimensionsElement.TryGetProperty("height", out var dimHeightElement) ? dimHeightElement.GetDecimal() : 0,
                    Depth = apiProduct.TryGetProperty("dimensions", out dimensionsElement) && dimensionsElement.TryGetProperty("depth", out var dimDepthElement) ? dimDepthElement.GetDecimal() : 0
                };

                var metaData = new MetaData
                {
                    CreatedAt = apiProduct.TryGetProperty("meta", out var metaElement) && metaElement.TryGetProperty("createdAt", out var metaCreatedAtElement) ? DateTime.Parse(metaCreatedAtElement.GetString()) : default,
                    UpdatedAt = metaElement.TryGetProperty("updatedAt", out var metaUpdatedAtElement) ? DateTime.Parse(metaUpdatedAtElement.GetString()) : default,
                    Barcode = metaElement.TryGetProperty("barcode", out var metaBarcodeElement) ? metaBarcodeElement.GetString() : string.Empty,
                    QrCode = metaElement.TryGetProperty("qrCode", out var metaQrCodeElement) ? metaQrCodeElement.GetString() : string.Empty
                };

                var product = new Product
                {
                    ProductId = apiProduct.TryGetProperty("id", out var productIdElement) ? productIdElement.GetInt32().ToString() : string.Empty,
                    Name = apiProduct.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : string.Empty,
                    Description = apiProduct.TryGetProperty("description", out var descriptionElement) ? descriptionElement.GetString() : string.Empty,
                    Price = apiProduct.TryGetProperty("price", out var priceElement) ? priceElement.GetDecimal() * usdToKesRate : 0,
                    Category = apiProduct.TryGetProperty("category", out var categoryElement) ? categoryElement.GetString() : string.Empty,
                    ImageUrl = apiProduct.TryGetProperty("thumbnail", out var thumbnailElement) ? thumbnailElement.GetString() : string.Empty,
                    Rating = apiProduct.TryGetProperty("rating", out var productRatingElement) ? productRatingElement.GetDouble() : 0,
                    RatingCount = reviews.Count,
                    DataSource = "DummyJson",
                    DiscountPercentage = apiProduct.TryGetProperty("discountPercentage", out var discountPercentageElement) ? discountPercentageElement.GetDecimal() : 0,
                    Stock = apiProduct.TryGetProperty("stock", out var stockElement) ? stockElement.GetInt32() : 0,
                    Tags = tags,
                    Brand = apiProduct.TryGetProperty("brand", out var brandElement) ? brandElement.GetString() : string.Empty,
                    SKU = apiProduct.TryGetProperty("sku", out var skuElement) ? skuElement.GetString() : string.Empty,
                    Weight = apiProduct.TryGetProperty("weight", out var weightElement) ? weightElement.GetDecimal() : 0,
                    Dimensions = dimensions,
                    WarrantyInformation = apiProduct.TryGetProperty("warrantyInformation", out var warrantyElement) ? warrantyElement.GetString() : string.Empty,
                    ShippingInformation = apiProduct.TryGetProperty("shippingInformation", out var shippingElement) ? shippingElement.GetString() : string.Empty,
                    AvailabilityStatus = apiProduct.TryGetProperty("availabilityStatus", out var availabilityElement) ? availabilityElement.GetString() : string.Empty,
                    Reviews = reviews,
                    ReturnPolicy = apiProduct.TryGetProperty("returnPolicy", out var returnPolicyElement) ? returnPolicyElement.GetString() : string.Empty,
                    MinimumOrderQuantity = apiProduct.TryGetProperty("minimumOrderQuantity", out var minOrderElement) ? minOrderElement.GetInt32() : 0,
                    Meta = metaData,
                    Images = images
                };

                products.Add(product);
            }

            return products;
        }


        private async Task<ProductResponse> FetchProductsFromEbay()
        {
            using var httpClient = new HttpClient();
            var apiUrl = _configuration["ExternalApis:EbayApiUrl"];
            var usdToKesRate = decimal.Parse(_configuration["CurrencySettings:UsdToKesRate"]);
            var ebayToken = _configuration["ExternalApis:EbayToken"];

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ebayToken}");

            //headers

            var response = await httpClient.GetAsync(apiUrl);
            var responseData = await response.Content.ReadAsStringAsync();

            var ebayData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseData);

            var productsList = JsonSerializer.Deserialize<List<dynamic>>(ebayData["merchandisedProducts"].ToString());

            //var metadata = new RequestMetadata
            //{
            //    TraceId = response.Headers.GetValues("traceid").FirstOrDefault(),
            //    EbayRequestId = response.Headers.GetValues("x-ebay-request-id").FirstOrDefault(),
            //    RlogId = response.Headers.GetValues("rlogid").FirstOrDefault(),
            //    RequestDate = DateTime.UtcNow
            //};

            var products = productsList.Select(apiProduct => new Product
            {
                ProductId = apiProduct.epid,
                Name = apiProduct.title,
                ImageUrl = apiProduct.image.imageUrl,
                Rating = apiProduct.averageRating,
                RatingCount = apiProduct.reviewCount,
                DataSource = "eBay",

                ConditionPrices = ((IEnumerable<dynamic>)apiProduct.marketPriceDetails)
                    .Select(detail => new ConditionPrice
                    {
                        ConditionGroup = detail.conditionGroup,
                        Price = decimal.Parse(detail.estimatedStartPrice.value) * usdToKesRate,
                        Currency = detail.estimatedStartPrice.currency
                    }).ToList(),

                RatingAspects = ((IEnumerable<dynamic>)apiProduct.ratingAspects)
                    .Select(aspect => new RatingAspect
                    {
                        Name = aspect.name,
                        Description = aspect.description,
                        Count = aspect.count,
                        Distributions = ((IEnumerable<dynamic>)aspect.ratingAspectDistributions)
                            .Select(dist => new RatingAspectDistribution
                            {
                                Value = dist.value,
                                Count = dist.count,
                                Percentage = double.Parse(dist.percentage)
                            }).ToList()
                    }).ToList(),

                VendorSpecificData = new SourceData
                {
                    Vendor = "eBay",
                    ExtraFields = new Dictionary<string, string>()
                }
            }).ToList();

            return new ProductResponse
            {
                MerchandisedProducts = products,
                //Metadata = metadata
            };
        }


        //private async Task<List<Product>> FetchProductsFromDummyJsonVendorApi()
        //{
        //    using var httpClient = new HttpClient();
        //    var apiUrl = _configuration["ExternalApis:DummyJsonApiUrl"];
        //    var usdToKesRate = decimal.Parse(_configuration["CurrencySettings:UsdToKesRate"]);

        //    var response = await httpClient.GetStringAsync(apiUrl);
        //    var productsFromApi = JsonSerializer.Deserialize<Dictionary<string, List<dynamic>>>(response);

        //    return productsFromApi["products"].Select(apiProduct => new Product
        //    {
        //        ProductId = apiProduct.GetProperty("id").GetInt32().ToString(),
        //        Name = apiProduct.GetProperty("title").GetString(),
        //        Description = apiProduct.GetProperty("description").GetString(),
        //        Price = apiProduct.GetProperty("price").GetDecimal() * usdToKesRate,
        //        Category = apiProduct.GetProperty("category").GetString(),
        //        ImageUrl = apiProduct.GetProperty("thumbnail").GetString(),
        //        Rating = apiProduct.GetProperty("rating").GetDouble(),
        //        RatingCount = apiProduct.GetProperty("reviews").GetArrayLength(),
        //        DataSource = "DummyJson"
        //    }).ToList();
        //}


    }
}



//two
/*
namespace EcommerceMiddleware.Controllers.ProductsController
{
     [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly EcommerceDbContext _context;

        private readonly IDistributedCache _cache;

        private readonly IConfiguration _configuration;

        public ProductsController(EcommerceDbContext context, IDistributedCache cache, IConfiguration configuration)
        {
            _context = context;
            _cache = cache;
            _configuration = configuration;
        }

//llowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetProductData()
        {
            var cacheKey = "vendor_products";
            string cachedProducts = await _cache.GetStringAsync(cacheKey);
            List<Product> products;

            if (!string.IsNullOrEmpty(cachedProducts))
            {
                // Deserialize products from cache
                products = JsonSerializer.Deserialize<List<Product>>(cachedProducts);
            }
            else
            {
                // Fetch products from vendor API (mocked here)
                products = await FetchAllProductsFromVendorApi();

                // Serialize and store products in Redis cache
                var serializedProducts = JsonSerializer.Serialize(products);
                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10)); // Cache expiration

                await _cache.SetStringAsync(cacheKey, serializedProducts, cacheOptions);
            }

            return Ok(products);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var cacheKey = "vendor_products";
            string cachedProducts = await _cache.GetStringAsync(cacheKey);
            List<Product> products;

            if (!string.IsNullOrEmpty(cachedProducts))
            {
                products = JsonSerializer.Deserialize<List<Product>>(cachedProducts);
            }
            else
            {
                // Fetch products from both vendors APIs and store
                products = await FetchProductsFromFakeStoreVendorApi();

                // Serialize and cache the data
                var serializedProducts = JsonSerializer.Serialize(products);
                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(60)); // Cache for 10 minutes

                await _cache.SetStringAsync(cacheKey, serializedProducts, cacheOptions);
            }

            var product = products.FirstOrDefault(p => int.Parse(p.ProductId) == id);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }



        // Helper method to simulate fetching products from vendor API
        private async Task<List<Product>> FetchProductsFromFakeStoreVendorApi()
        {
            using var httpClient = new HttpClient();
            //var response = await httpClient.GetStringAsync("https://fakestoreapi.com/products");

            var apiUrl = _configuration["ExternalApis:FakeStoreApiUrl"];
            var usdToKesRate = decimal.Parse(_configuration["CurrencySettings:UsdToKesRate"]);
            var response = await httpClient.GetStringAsync(apiUrl);

            // Deserialize into the structure matching FakeStore's response
            var productsFromApi = JsonSerializer.Deserialize<List<dynamic>>(response);

            // Map to our Product model
            return productsFromApi.Select(apiProduct => new Product
            {
                ProductId = apiProduct.GetProperty("id").GetInt32(), // Access id as an integer
                Name = apiProduct.GetProperty("title").GetString(), // Access title as a string
                Description = apiProduct.GetProperty("description").GetString(), // Access description
                Price = apiProduct.GetProperty("price").GetDecimal() * usdToKesRate, // Access price
                Category = apiProduct.GetProperty("category").GetString(), // Access category
                ImageUrl = apiProduct.GetProperty("image").GetString(), // Access image
                Rating = apiProduct.GetProperty("rating").GetProperty("rate").GetDouble(), // Access rating rate
                RatingCount = apiProduct.GetProperty("rating").GetProperty("count").GetInt32(), // Access rating count
                DataSource = "FakeStore"
            }).ToList();
        }

        private async Task<ProductResponse> FetchProductsFromEbay()
        {
            using var httpClient = new HttpClient();
            var apiUrl = _configuration["ExternalApis:EbayApiUrl"];
            var usdToKesRate = decimal.Parse(_configuration["CurrencySettings:UsdToKesRate"]);
            var ebayToken = _configuration["ExternalApis:EbayToken"];

            // Add request headers
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ebayToken}");

            var response = await httpClient.GetAsync(apiUrl);
            var responseData = await response.Content.ReadAsStringAsync();

            // Deserialize the JSON response
            var ebayData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseData);

            var productsList = JsonSerializer.Deserialize<List<dynamic>>(ebayData["merchandisedProducts"].ToString());

            // Extract header information for metadata
            var metadata = new RequestMetadata
            {
                TraceId = response.Headers.GetValues("traceid").FirstOrDefault(),
                EbayRequestId = response.Headers.GetValues("x-ebay-request-id").FirstOrDefault(),
                RlogId = response.Headers.GetValues("rlogid").FirstOrDefault(),
                RequestDate = DateTime.UtcNow
            };

            // Map each item in merchandisedProducts
            var products = productsList.Select(apiProduct => new Product
            {
                ProductId = apiProduct.epid,
                Name = apiProduct.title,
                ImageUrl = apiProduct.image.imageUrl,
                Rating = apiProduct.averageRating,
                RatingCount = apiProduct.reviewCount,
                DataSource = "eBay",

                ConditionPrices = ((IEnumerable<dynamic>)apiProduct.marketPriceDetails)
                    .Select(detail => new ConditionPrice
                    {
                        ConditionGroup = detail.conditionGroup,
                        Price = decimal.Parse(detail.estimatedStartPrice.value) * usdToKesRate,
                        Currency = detail.estimatedStartPrice.currency
                    }).ToList(),

                RatingAspects = ((IEnumerable<dynamic>)apiProduct.ratingAspects)
                    .Select(aspect => new RatingAspect
                    {
                        Name = aspect.name,
                        Description = aspect.description,
                        Count = aspect.count,
                        Distributions = ((IEnumerable<dynamic>)aspect.ratingAspectDistributions)
                            .Select(dist => new RatingAspectDistribution
                            {
                                Value = dist.value,
                                Count = dist.count,
                                Percentage = double.Parse(dist.percentage)
                            }).ToList()
                    }).ToList(),

                VendorSpecificData = new SourceData
                {
                    Vendor = "eBay",
                    ExtraFields = new Dictionary<string, string>()
                }
            }).ToList();

            // Return the response with both products and metadata
            return new ProductResponse
            {
                MerchandisedProducts = products,
                Metadata = metadata
            };
        }


    }

}
*/


//original

//// GET: api/products
//[HttpGet]
//    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
//    {
//        return await _context.Products.ToListAsync();
//    }

//    // GET: api/products/{id}
//    [HttpGet("{id}")]
//    public async Task<ActionResult<Product>> GetProduct(int id)
//    {
//        var product = await _context.Products.FindAsync(id);
//        if (product == null)
//        {
//            return NotFound();
//        }
//        return product;
//    }

//    // POST: api/products
//    [HttpPost]
//    public async Task<ActionResult<Product>> CreateProduct(Product product)
//    {
//        _context.Products.Add(product);
//        await _context.SaveChangesAsync();

//        return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
//    }

//    // PUT: api/products/{id}
//    [HttpPut("{id}")]
//    public async Task<IActionResult> UpdateProduct(int id, Product product)
//    {
//        if (id != product.ProductId)
//        {
//            return BadRequest();
//        }

//        _context.Entry(product).State = EntityState.Modified;
//        await _context.SaveChangesAsync();

//        return NoContent();
//    }

//    // DELETE: api/products/{id}
//    [HttpDelete("{id}")]
//    public async Task<IActionResult> DeleteProduct(int id)
//    {
//        var product = await _context.Products.FindAsync(id);
//        if (product == null)
//        {
//            return NotFound();
//        }

//        _context.Products.Remove(product);
//        await _context.SaveChangesAsync();

//        return NoContent();
//    }
//}



