using System.ComponentModel.DataAnnotations;

namespace RedisCrudAPI.DTOs
{
    public class ProductDTO
    {

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Category { get; set; } = string.Empty;
        //public string? ImageUrl { get; set; }
        public string Sku { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }





    public class CreateProductDTO
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        //[Url]
        //public string? ImageUrl { get; set; }

        [Required]
        public string Sku { get; set; } = string.Empty;
    }

    public class UpdateProductDTO
    {
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Price { get; set; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        //[Url]
        //public string? ImageUrl { get; set; }

        public bool? IsActive { get; set; }
    }
}
