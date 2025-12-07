using RedisCrudAPI.DTOs;

namespace RedisCrudAPI.Services
{
    public interface IProductService
    {
        Task<ProductDTO?> GetProductByIdAsync(int id);
        Task<IEnumerable<ProductDTO>> GetAllProductsAsync();
        Task<IEnumerable<ProductDTO>> GetActiveProductsAsync();
        Task<IEnumerable<ProductDTO>> GetProductsByCategoryAsync(string category);
        Task<ProductDTO> CreateProductAsync(CreateProductDTO createDto);
        Task<ProductDTO?> UpdateProductAsync(int id, UpdateProductDTO updateDto);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> UpdateStockAsync(int productId, int quantity);
        Task<int> GetProductStockAsync(int productId);
        Task ClearProductCacheAsync(int? productId = null);
        Task<ProductStatsDTO> GetProductStatsAsync();
    }

    public class ProductStatsDTO
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public Dictionary<string, int> ProductsByCategory { get; set; } = new();
        public decimal TotalInventoryValue { get; set; }
    }
}