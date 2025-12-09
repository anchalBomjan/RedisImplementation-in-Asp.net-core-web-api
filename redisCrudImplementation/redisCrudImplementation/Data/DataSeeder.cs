using redisCrudImplementation.Models;
using System;

namespace redisCrudImplementation.Data
{
    public static class DataSeeder
    {
        public static void SeedDatabase(ApplicationDbContext context)
        {
            if (!context.Products.Any())
            {
                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "Laptop",
                        Description = "High performance laptop with 16GB RAM",
                        Price = 999.99m,
                        StockQuantity = 10
                    },
                    new Product
                    {
                        Name = "Smartphone",
                        Description = "Latest smartphone with 128GB storage",
                        Price = 699.99m,
                        StockQuantity = 25
                    },
                    new Product
                    {
                        Name = "Headphones",
                        Description = "Wireless noise-cancelling headphones",
                        Price = 199.99m,
                        StockQuantity = 50
                    },
                    new Product
                    {
                        Name = "Keyboard",
                        Description = "Mechanical gaming keyboard",
                        Price = 89.99m,
                        StockQuantity = 30
                    },
                    new Product
                    {
                        Name = "Monitor",
                        Description = "27-inch 4K monitor",
                        Price = 349.99m,
                        StockQuantity = 15
                    },
                    new Product
                    {
                        Name = "Mouse",
                        Description = "Wireless gaming mouse",
                        Price = 59.99m,
                        StockQuantity = 40
                    },
                    new Product
                    {
                        Name = "Tablet",
                        Description = "10-inch tablet with stylus",
                        Price = 449.99m,
                        StockQuantity = 20
                    },
                    new Product
                    {
                        Name = "Smart Watch",
                        Description = "Fitness tracker with heart rate monitor",
                        Price = 199.99m,
                        StockQuantity = 35
                    },
                    new Product
                    {
                        Name = "Speaker",
                        Description = "Bluetooth portable speaker",
                        Price = 129.99m,
                        StockQuantity = 45
                    },
                    new Product
                    {
                        Name = "Webcam",
                        Description = "HD webcam with microphone",
                        Price = 79.99m,
                        StockQuantity = 60
                    }
                };

                context.Products.AddRange(products);
                context.SaveChanges();

                Console.WriteLine("✅ Database seeded with 10 products");
            }
        }
    }
    }
