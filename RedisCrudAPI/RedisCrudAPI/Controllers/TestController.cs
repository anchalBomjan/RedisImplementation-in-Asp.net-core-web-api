using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedisCrudAPI.Data;

namespace RedisCrudAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("db")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    var productsCount = await _context.Products.CountAsync();

                    return Ok(new
                    {
                        Status = "Database connection successful",
                        ProductsCount = productsCount,
                        DatabaseName = _context.Database.GetDbConnection().Database,
                        Server = _context.Database.GetDbConnection().DataSource
                    });
                }

                return BadRequest("Cannot connect to database");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "Database connection failed",
                    Message = ex.Message,
                    Details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                Message = "API is running",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            });
        }
    }
}
