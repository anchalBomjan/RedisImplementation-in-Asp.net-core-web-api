using Microsoft.AspNetCore.Mvc;
using redisCrudImplementation.DTOs;
using redisCrudImplementation.Services.Interface;

namespace redisCrudImplementation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetAll()
        {
            try
            {
                var products = await _productService.GetAllAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            try
            {
                var product = await _productService.GetByIdAsync(id);
                if (product == null)
                    return NotFound($"Product with ID {id} not found");

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting product {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create(CreateProductDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var product = await _productService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateProductDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var product = await _productService.UpdateAsync(id, updateDto);
                if (product == null)
                    return NotFound($"Product with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating product {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _productService.DeleteAsync(id);
                if (!deleted)
                    return NotFound($"Product with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/products/test-cache
        [HttpGet("test-cache")]
        public async Task<ActionResult> TestCache()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var products = await _productService.GetAllAsync();
                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalMilliseconds;

                return Ok(new
                {
                    Message = "Cache Test Complete",
                    ProductCount = products.Count,
                    DurationMs = duration,
                    Source = duration < 50 ? "Redis Cache" : "Database"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing cache");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
