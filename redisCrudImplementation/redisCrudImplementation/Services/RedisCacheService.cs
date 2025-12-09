using Microsoft.Extensions.Caching.Distributed;
using redisCrudImplementation.Services.Interface;
using System.Text.Json;

namespace redisCrudImplementation.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(5);

        public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            Console.WriteLine("✅ RedisCacheService created");
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                Console.WriteLine($"🔍 GET from Redis - Key: {key}");

                var cachedData = await _cache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cachedData))
                {
                    Console.WriteLine($"❌ CACHE MISS for key: {key}");
                    _logger.LogDebug($"Cache miss for key: {key}");
                    return default;
                }

                Console.WriteLine($"✅ CACHE HIT for key: {key}");
                Console.WriteLine($"📦 Data length: {cachedData.Length} characters");
                _logger.LogDebug($"Cache hit for key: {key}");

                return JsonSerializer.Deserialize<T>(cachedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Redis GET error for key {key}: {ex.Message}");
                _logger.LogError(ex, $"Error getting cache for key: {key}");
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry ?? _defaultExpiry
                };

                var serializedData = JsonSerializer.Serialize(value);
                Console.WriteLine($"💾 SET to Redis - Key: {key}, Expiry: {options.AbsoluteExpirationRelativeToNow}");
                Console.WriteLine($"📦 Serialized data length: {serializedData.Length} characters");

                await _cache.SetStringAsync(key, serializedData, options);

                Console.WriteLine($"✅ Successfully cached: {key}");
                _logger.LogDebug($"Cache set for key: {key}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Redis SET error for key {key}: {ex.Message}");
                _logger.LogError(ex, $"Error setting cache for key: {key}");
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                Console.WriteLine($"🗑️  REMOVE from Redis - Key: {key}");
                await _cache.RemoveAsync(key);
                Console.WriteLine($"✅ Removed from cache: {key}");
                _logger.LogDebug($"Cache removed for key: {key}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Redis REMOVE error for key {key}: {ex.Message}");
                _logger.LogError(ex, $"Error removing cache for key: {key}");
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var data = await _cache.GetStringAsync(key);
                var exists = !string.IsNullOrEmpty(data);
                Console.WriteLine($"🔎 EXISTS check - Key: {key}, Result: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Redis EXISTS error: {ex.Message}");
                return false;
            }
        }
    }
}
