using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RedisCrudAPI.Data;
using RedisCrudAPI.DTOs;
using RedisCrudAPI.Models;
using RedisCrudAPI.Repositories;

namespace RedisCrudAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRedisService _redisService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IRepository<Product> productRepository,
            IRedisService redisService,
            AppDbContext context,
            IMapper mapper,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _redisService = redisService;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ProductDTO?> GetProductByIdAsync(int id)
        {
            var cacheKey = RedisService.CacheKeys.Product(id);

            var cachedProduct = await _redisService.GetAsync<ProductDTO>(cacheKey);
            if (cachedProduct != null)
            {
                _logger.LogDebug("Cache hit for product {Id}", id);
                return cachedProduct;
            }

            _logger.LogDebug("Cache miss for product {Id}", id);

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return null;
            }

            var productDto = _mapper.Map<ProductDTO>(product);

            await _redisService.SetAsync(cacheKey, productDto);

            return productDto;
        }

        public async Task<IEnumerable<ProductDTO>> GetAllProductsAsync()
        {
            var cacheKey = RedisService.CacheKeys.ProductsAll;

            var cachedProducts = await _redisService.GetAsync<List<ProductDTO>>(cacheKey);
            if (cachedProducts != null)
            {
                _logger.LogDebug("Cache hit for all products");
                return cachedProducts;
            }

            _logger.LogDebug("Cache miss for all products");

            var products = await _productRepository.GetAllAsync();
            var productDtos = _mapper.Map<List<ProductDTO>>(products);

            await _redisService.SetAsync(cacheKey, productDtos);

            return productDtos;
        }

        public async Task<IEnumerable<ProductDTO>> GetActiveProductsAsync()
        {
            var cacheKey = RedisService.CacheKeys.ProductsActive;

            var cachedProducts = await _redisService.GetAsync<List<ProductDTO>>(cacheKey);
            if (cachedProducts != null)
            {
                _logger.LogDebug("Cache hit for active products");
                return cachedProducts;
            }

            _logger.LogDebug("Cache miss for active products");

            var products = await _productRepository.FindAsync(p => p.IsActive && !p.IsDeleted);
            var productDtos = _mapper.Map<List<ProductDTO>>(products);

            await _redisService.SetAsync(cacheKey, productDtos);

            return productDtos;
        }

        public async Task<IEnumerable<ProductDTO>> GetProductsByCategoryAsync(string category)
        {
            var cacheKey = RedisService.CacheKeys.ProductsByCategory(category.ToLower());

            var cachedProducts = await _redisService.GetAsync<List<ProductDTO>>(cacheKey);
            if (cachedProducts != null)
            {
                _logger.LogDebug("Cache hit for category {Category}", category);
                return cachedProducts;
            }

            _logger.LogDebug("Cache miss for category {Category}", category);

            var products = await _productRepository.FindAsync(p =>
                p.Category.ToLower() == category.ToLower() &&
                p.IsActive &&
                !p.IsDeleted);

            var productDtos = _mapper.Map<List<ProductDTO>>(products);

            await _redisService.SetAsync(cacheKey, productDtos);

            return productDtos;
        }

        public async Task<ProductDTO> CreateProductAsync(CreateProductDTO createDto)
        {
            var product = _mapper.Map<Product>(createDto);

            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Sku == createDto.Sku);

            if (existingProduct != null)
            {
                throw new InvalidOperationException($"Product with SKU '{createDto.Sku}' already exists");
            }

            product.CreatedAt = DateTime.UtcNow;
            product.IsActive = true;

            var createdProduct = await _productRepository.AddAsync(product);

            await ClearProductCacheAsync();

            _logger.LogInformation("Created product {Id} with SKU {Sku}", createdProduct.Id, createdProduct.Sku);

            return _mapper.Map<ProductDTO>(createdProduct);
        }

        public async Task<ProductDTO?> UpdateProductAsync(int id, UpdateProductDTO updateDto)
        {
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(updateDto.Name))
                existingProduct.Name = updateDto.Name;

            if (!string.IsNullOrEmpty(updateDto.Description))
                existingProduct.Description = updateDto.Description;

            if (updateDto.Price.HasValue)
                existingProduct.Price = updateDto.Price.Value;

            if (updateDto.StockQuantity.HasValue)
                existingProduct.StockQuantity = updateDto.StockQuantity.Value;

            if (!string.IsNullOrEmpty(updateDto.Category))
                existingProduct.Category = updateDto.Category;

            //if (updateDto.ImageUrl != null)
            //    existingProduct.ImageUrl = updateDto.ImageUrl;

            if (updateDto.IsActive.HasValue)
                existingProduct.IsActive = updateDto.IsActive.Value;

            existingProduct.UpdatedAt = DateTime.UtcNow;

            var updatedProduct = await _productRepository.UpdateAsync(existingProduct);

            await ClearProductCacheAsync(id);

            _logger.LogInformation("Updated product {Id}", id);

            return _mapper.Map<ProductDTO>(updatedProduct);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var result = await _productRepository.DeleteAsync(id);

            if (result)
            {
                await ClearProductCacheAsync(id);
                _logger.LogInformation("Soft deleted product {Id}", id);
            }

            return result;
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return false;
            }

            product.StockQuantity += quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);

            var cacheKey = RedisService.CacheKeys.ProductStock(productId);
            await _redisService.SetAsync(cacheKey, product.StockQuantity);

            await ClearProductCacheAsync(productId);

            _logger.LogInformation("Updated stock for product {Id} by {Quantity}", productId, quantity);

            return true;
        }

        public async Task<int> GetProductStockAsync(int productId)
        {
            var cacheKey = RedisService.CacheKeys.ProductStock(productId);

            var cachedStockNullable = await _redisService.GetAsync<int?>(cacheKey);

            if (cachedStockNullable.HasValue)
            {
                return cachedStockNullable.Value;
            }

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return 0;
            }

            await _redisService.SetAsync(cacheKey, product.StockQuantity);

            return product.StockQuantity;
        }

        public async Task ClearProductCacheAsync(int? productId = null)
        {
            try
            {
                if (productId.HasValue)
                {
                    await _redisService.RemoveAsync(RedisService.CacheKeys.Product(productId.Value));
                    await _redisService.RemoveAsync(RedisService.CacheKeys.ProductStock(productId.Value));
                }

                await _redisService.RemoveAsync(RedisService.CacheKeys.ProductsAll);
                await _redisService.RemoveAsync(RedisService.CacheKeys.ProductsActive);

                _logger.LogInformation("Product cache cleared for product {ProductId}", productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing product cache");
            }
        }

        public async Task<ProductStatsDTO> GetProductStatsAsync()
        {
            var cacheKey = RedisService.CacheKeys.ApiStats;

            var cachedStats = await _redisService.GetAsync<ProductStatsDTO>(cacheKey);
            if (cachedStats != null)
            {
                return cachedStats;
            }

            var stats = new ProductStatsDTO();

            try
            {
                var products = await _context.Products
                    .Where(p => !p.IsDeleted)
                    .ToListAsync();

                stats.TotalProducts = products.Count;
                stats.ActiveProducts = products.Count(p => p.IsActive);
                stats.OutOfStockProducts = products.Count(p => p.StockQuantity == 0);
                stats.TotalInventoryValue = products.Sum(p => p.Price * p.StockQuantity);

                stats.ProductsByCategory = products
                    .GroupBy(p => p.Category)
                    .ToDictionary(g => g.Key, g => g.Count());

                await _redisService.SetAsync(cacheKey, stats, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product stats");
                return stats;
            }
        }
    }
}
