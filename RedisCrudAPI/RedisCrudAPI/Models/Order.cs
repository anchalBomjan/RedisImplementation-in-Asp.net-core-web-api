using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RedisCrudAPI.Models
{
    public class Order : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        [StringLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled

        public DateTime? OrderDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }


 }
