using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RedisCrudAPI.Models
{



        public class Product : BaseEntity
        {
            [Key]
            public int Id { get; set; }

            [Required]
            [StringLength(200)]
            public string Name { get; set; } = string.Empty;

            [StringLength(500)]
            public string Description { get; set; } = string.Empty;

            [Column(TypeName = "decimal(18,2)")]
            public decimal Price { get; set; }

            public int StockQuantity { get; set; }

            [StringLength(100)]
            public string Category { get; set; } = string.Empty;

            //public string? ImageUrl { get; set; }

            public string Sku { get; set; } = string.Empty;

            public bool IsActive { get; set; } = true;

            // Navigation properties
            public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        }
}
