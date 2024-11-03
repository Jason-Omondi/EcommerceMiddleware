

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceMiddleware.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.OpenApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;



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
                products = await FetchProductsFromVendorApi();

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
                // Fetch products from vendor API
                products = await FetchProductsFromVendorApi();

                // Serialize and cache the data
                var serializedProducts = JsonSerializer.Serialize(products);
                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10)); // Cache for 10 minutes

                await _cache.SetStringAsync(cacheKey, serializedProducts, cacheOptions);
            }

            var product = products.FirstOrDefault(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }



        // Helper method to simulate fetching products from vendor API
        private async Task<List<Product>> FetchProductsFromVendorApi()
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

                //ProductId = apiProduct.id,
                //Name = apiProduct.title,
                //Description = apiProduct.description,
                //Price = (decimal)apiProduct.price * usdToKesRate,
                //Category = apiProduct.category,
                //ImageUrl = apiProduct.image,
                ////Rating = (double)apiProduct.rating.rate,
                ////RatingCount = (int)apiProduct.rating.count,
                //Rating = apiProduct.rating.rate,
                //RatingCount = apiProduct.rating.count,
                //DataSource = "FakeStore"
            }).ToList();
        }



    }

}
    
    
    
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

   

