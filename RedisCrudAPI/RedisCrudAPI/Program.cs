// Program.cs - UPDATED WITH PROPER DI
using Microsoft.EntityFrameworkCore;
using RedisCrudAPI.Data;
using RedisCrudAPI.Repositories;
using RedisCrudAPI.Services;
using RedisCrudAPI.Settings;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ IMPORTANT: Get connection strings first
var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

if (string.IsNullOrEmpty(sqlConnectionString))
    throw new InvalidOperationException("SQL Server connection string is not configured");

Console.WriteLine($"SQL Connection: {sqlConnectionString}");
Console.WriteLine($"Redis Connection: {redisConnectionString}");

// ✅ Configure SQL Server (FIXED: AddDbContext)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

// ✅ Configure Redis
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "RedisCrud_";
    });
}
else
{
    Console.WriteLine("⚠️ Redis connection string not found. Using in-memory cache instead.");
    builder.Services.AddDistributedMemoryCache();
}

// ✅ Configure Redis Settings
builder.Services.Configure<RedisSettings>(
    builder.Configuration.GetSection("RedisSettings"));

// ✅ REGISTER ALL SERVICES IN CORRECT ORDER
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies()); // ✅ FIXED
builder.Services.AddScoped(typeof(IRepository<>), typeof(CachedRepository<>));
builder.Services.AddScoped<IProductService, ProductService>();

// ✅ Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage(); // ✅ Shows detailed errors
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ✅ Simple test endpoint
app.MapGet("/test", () => Results.Ok(new
{
    message = "API is running",
    time = DateTime.UtcNow
}));

// ✅ Initialize Database
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync(); // ✅ Simpler than Migrate
    Console.WriteLine("✅ Database initialized");

    // Check if we have products
    var productCount = await context.Products.CountAsync();
    Console.WriteLine($"✅ Found {productCount} products in database");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database error: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
}

app.Run();