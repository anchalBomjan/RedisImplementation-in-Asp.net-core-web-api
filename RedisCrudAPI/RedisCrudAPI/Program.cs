//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();
// Program.cs - Updated with proper DI configuration
using Microsoft.EntityFrameworkCore;
using RedisCrudAPI.Data;
using RedisCrudAPI.Settings;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Redis CRUD API",
        Version = "v1",
        Description = "ASP.NET Core Web API with Redis Caching and SQL Server"
    });
});

// ?? CRITICAL: Configure DbContext with SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

    // Enable sensitive data logging only in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configure Redis Caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = builder.Configuration["RedisSettings:InstanceName"] ?? "RedisCrud_";
});

// Configure Redis Settings
builder.Services.Configure<RedisSettings>(
    builder.Configuration.GetSection("RedisSettings"));

// Register Services with Dependency Injection
//builder.Services.AddScoped<IRedisService, RedisService>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Register Repositories
//builder.Services.AddScoped(typeof(IRepository<>), typeof(CachedRepository<>));

// Register Application Services
//builder.Services.AddScoped<IProductService, ProductService>();

// Add Health Checks
//builder.Services.AddHealthChecks()
 //   .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!)
 //   .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");

// Initialize Database on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Apply pending migrations
        await context.Database.MigrateAsync();

        Log.Information("Database migrated successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while migrating the database");
    }
}

app.Run();