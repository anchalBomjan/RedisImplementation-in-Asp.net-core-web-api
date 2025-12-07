using Microsoft.EntityFrameworkCore;
using RedisCrudAPI.Models;

namespace RedisCrudAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
           : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(p => p.Sku).IsUnique();
                entity.HasIndex(p => p.Category);
                entity.HasIndex(p => p.Name);

                entity.Property(p => p.Sku)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.Price)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(p => p.StockQuantity)
                    .HasDefaultValue(0);

                entity.HasQueryFilter(p => !p.IsDeleted);
            });

            // Configure Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasIndex(o => o.OrderNumber).IsUnique();
                entity.HasIndex(o => o.CustomerEmail);
                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => o.OrderDate);

                entity.Property(o => o.OrderNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(o => o.TotalAmount)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(o => o.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("Pending");

                entity.HasQueryFilter(o => !o.IsDeleted);
            });

            // Configure OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(oi => oi.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(oi => oi.TotalPrice)
                    .HasColumnType("decimal(18,2)")
                    .HasComputedColumnSql("[Quantity] * [UnitPrice]");
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Products
            var products = new List<Product>
            {
                new Product
                {
                    Id = 1,
                    Name = "Apple MacBook Pro 16\"",
                    Description = "16-inch MacBook Pro with M2 Pro chip",
                    Price = 2499.99m,
                    StockQuantity = 50,
                    Category = "Electronics",
                    Sku = "MBP16-M2-001",
                  //  ImageUrl = "https://example.com/macbook.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 2,
                    Name = "iPhone 15 Pro",
                    Description = "Latest iPhone with A17 Pro chip",
                    Price = 999.99m,
                    StockQuantity = 100,
                    Category = "Electronics",
                    Sku = "IP15-PRO-001",
                  //  ImageUrl = "https://example.com/iphone.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 3,
                    Name = "Sony WH-1000XM5",
                    Description = "Noise cancelling headphones",
                    Price = 399.99m,
                    StockQuantity = 75,
                    Category = "Electronics",
                    Sku = "SONY-XM5-001",
                  //  ImageUrl = "https://example.com/headphones.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 4,
                    Name = "Nike Air Max 270",
                    Description = "Comfortable running shoes",
                    Price = 149.99m,
                    StockQuantity = 200,
                    Category = "Fashion",
                    Sku = "NIKE-AM270-001",
                   // ImageUrl = "https://example.com/shoes.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            modelBuilder.Entity<Product>().HasData(products);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity &&
                    (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    entity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

    }
}
