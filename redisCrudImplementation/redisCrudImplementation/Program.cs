using Microsoft.EntityFrameworkCore;
using redisCrudImplementation.Data;
using redisCrudImplementation.Services;
using redisCrudImplementation.Services.Interface;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ ADD LOGGING TO SEE WHAT'S HAPPENING
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

Console.WriteLine("🚀 Starting Redis Demo API...");
Console.WriteLine($"Redis Connection: {builder.Configuration.GetConnectionString("Redis")}");

// Configure SQL Server
var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"SQL Connection: {sqlConnectionString}");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        sqlConnectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

// Configure Redis - FIXED CONFIGURATION
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
Console.WriteLine($"🔗 Connecting to Redis at: {redisConnectionString}");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "RedisCrud_";
});

// Register Services
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage(); // ✅ ADD THIS
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ✅ ADD TEST ENDPOINTS BEFORE app.Run()
app.MapGet("/", () =>
{
    Console.WriteLine("✅ Root endpoint called");
    return "Redis Demo API is running! Go to /api/products";
});

app.MapGet("/test-redis", async (IRedisCacheService redisService) =>
{
    try
    {
        Console.WriteLine("🧪 Testing Redis connection...");

        // Test Redis
        await redisService.SetAsync("test_key", "Hello Redis!", TimeSpan.FromMinutes(1));
        var value = await redisService.GetAsync<string>("test_key");

        return Results.Ok(new
        {
            message = "Redis Test",
            status = value != null ? "✅ Redis is working!" : "❌ Redis failed",
            testValue = value,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Redis test error: {ex.Message}");
        return Results.Problem($"Redis Error: {ex.Message}");
    }
});

// Create database and seed data
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Create database if not exists
    dbContext.Database.EnsureCreated();
    Console.WriteLine("✅ Database ensured created");

    // Seed data
    DataSeeder.SeedDatabase(dbContext);
    Console.WriteLine("✅ Database seeded (if needed)");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database error: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"❌ Inner error: {ex.InnerException.Message}");
}

Console.WriteLine("🎯 API is ready! Listening on https://localhost:5001");
app.Run();