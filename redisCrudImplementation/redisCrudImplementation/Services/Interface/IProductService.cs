using redisCrudImplementation.DTOs;

namespace redisCrudImplementation.Services.Interface
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllAsync();
        Task<ProductDto?> GetByIdAsync(int id);
        Task<ProductDto> CreateAsync(CreateProductDto createDto);
        Task<ProductDto?> UpdateAsync(int id, UpdateProductDto updateDto);
        Task<bool> DeleteAsync(int id);
    }
}
