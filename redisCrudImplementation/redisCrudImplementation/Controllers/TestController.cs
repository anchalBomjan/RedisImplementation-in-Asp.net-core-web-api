using Microsoft.AspNetCore.Mvc;
using redisCrudImplementation.Services.Interface;

namespace redisCrudImplementation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IRedisCacheService _redisService;

        public TestController(IRedisCacheService redisService)
        {
            _redisService = redisService;
        }

        [HttpGet("redis")]
        public async Task<IActionResult> TestRedis()
        {
            try
            {
                // Clear any existing test data
                await _redisService.RemoveAsync("test:simple");

                // Test 1: Set a value
                await _redisService.SetAsync("test:simple", new
                {
                    message = "Hello Redis!",
                    timestamp = DateTime.UtcNow
                });

                // Test 2: Get the value back
                var retrieved = await _redisService.GetAsync<object>("test:simple");

                // Test 3: Check if exists
                var exists = await _redisService.ExistsAsync("test:simple");

                // Test 4: Remove it
                await _redisService.RemoveAsync("test:simple");
                var removedExists = await _redisService.ExistsAsync("test:simple");

                return Ok(new
                {
                    Message = "Redis Test Complete",
                    Tests = new[]
                    {
                        new { Test = "Set Value", Result = "✅" },
                        new { Test = "Get Value", Result = retrieved != null ? "✅" : "❌" },
                        new { Test = "Exists Check", Result = exists ? "✅" : "❌" },
                        new { Test = "Remove Value", Result = !removedExists ? "✅" : "❌" }
                    },
                    RetrievedData = retrieved,
                    FinalStatus = exists && !removedExists ? "All tests passed!" : "Some tests failed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "Redis test failed",
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
                Redis = "localhost:6379",
                Status = "✅"
            });
        }
    }

}
