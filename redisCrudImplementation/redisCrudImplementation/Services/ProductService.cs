using Microsoft.EntityFrameworkCore;
using redisCrudImplementation.Data;
using redisCrudImplementation.DTOs;
using redisCrudImplementation.Models;
using redisCrudImplementation.Services.Interface;
using System;

namespace redisCrudImplementation.Services
{
    public class ProductService:IProductService
    {

        private readonly ApplicationDbContext _context;
        private readonly IRedisCacheService _cache;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
           ApplicationDbContext context,
            IRedisCacheService cache,
            ILogger<ProductService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // GET ALL Products with Caching
        public async Task<List<ProductDto>> GetAllAsync()
        {
            const string cacheKey = "all_products";

            // Try to get from cache
            var cachedProducts = await _cache.GetAsync<List<ProductDto>>(cacheKey);
            if (cachedProducts != null)
            {
                _logger.LogInformation("✅ Retrieved ALL products from Redis cache");
                return cachedProducts;
            }

            // Cache miss - get from database
            _logger.LogInformation("❌ Cache MISS for ALL products - Querying database...");

            var products = await _context.Products
                .OrderBy(p => p.Id)
                .ToListAsync();

            var productDtos = products.Select(MapToDto).ToList();

            // Store in cache for future requests
            await _cache.SetAsync(cacheKey, productDtos);
            _logger.LogInformation($"✅ Cached {productDtos.Count} products");

            return productDtos;
        }

        // GET Product by ID with Caching
        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var cacheKey = $"product_{id}";

            // Try to get from cache
            var cachedProduct = await _cache.GetAsync<ProductDto>(cacheKey);
            if (cachedProduct != null)
            {
                _logger.LogInformation($"✅ Retrieved product {id} from Redis cache");
                return cachedProduct;
            }

            // Cache miss - get from database
            _logger.LogInformation($"❌ Cache MISS for product {id} - Querying database...");

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return null;

            var productDto = MapToDto(product);

            // Store in cache
            await _cache.SetAsync(cacheKey, productDto);
            _logger.LogInformation($"✅ Cached product {id}");

            return productDto;
        }

        // CREATE Product (invalidate cache)
        public async Task<ProductDto> CreateAsync(CreateProductDto createDto)
        {
            var product = new Product
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Price = createDto.Price,
                StockQuantity = createDto.StockQuantity,
                CreatedDate = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Invalidate cache since we added new product
            await _cache.RemoveAsync("all_products");
            _logger.LogInformation("✅ Invalidated 'all_products' cache after CREATE");

            return MapToDto(product);
        }

        // UPDATE Product (invalidate cache)
        public async Task<ProductDto?> UpdateAsync(int id, UpdateProductDto updateDto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return null;

            product.Name = updateDto.Name;
            product.Description = updateDto.Description;
            product.Price = updateDto.Price;
            product.StockQuantity = updateDto.StockQuantity;
            product.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Invalidate caches
            await Task.WhenAll(
                _cache.RemoveAsync("all_products"),
                _cache.RemoveAsync($"product_{id}")
            );

            _logger.LogInformation($"✅ Invalidated caches for product {id} after UPDATE");

            return MapToDto(product);
        }

        // DELETE Product (invalidate cache)
        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            // Invalidate caches
            await Task.WhenAll(
                _cache.RemoveAsync("all_products"),
                _cache.RemoveAsync($"product_{id}")
            );

            _logger.LogInformation($"✅ Invalidated caches for product {id} after DELETE");

            return true;
        }

        // Helper method to map Product to ProductDto
        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CreatedDate = product.CreatedDate,
                UpdatedDate = product.UpdatedDate
            };
        }
    }
}
