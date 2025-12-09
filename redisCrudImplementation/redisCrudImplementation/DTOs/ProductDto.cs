using System.ComponentModel.DataAnnotations;

namespace redisCrudImplementation.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class CreateProductDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 10000)]
        public decimal Price { get; set; }

        [Range(0, 1000)]
        public int StockQuantity { get; set; }
    }

    public class UpdateProductDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 10000)]
        public decimal Price { get; set; }

        [Range(0, 1000)]
        public int StockQuantity { get; set; }
    }


}
