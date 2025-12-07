using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using RedisCrudAPI.Settings;
using System.Text.Json;

namespace RedisCrudAPI.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDistributedCache _cache;
        private readonly RedisSettings _redisSettings;
        private readonly ILogger<RedisService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisService(
            IDistributedCache cache,
            IOptions<RedisSettings> redisSettings,
            ILogger<RedisService> logger)
        {
            _cache = cache;
            _redisSettings = redisSettings.Value;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key, cancellationToken);

                if (string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                    return default;
                }

                _logger.LogDebug("Cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from Redis for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var serializedData = JsonSerializer.Serialize(value, _jsonOptions);

                options ??= new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(_redisSettings.SlidingExpirationMinutes),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_redisSettings.AbsoluteExpirationMinutes)
                };

                await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
                _logger.LogDebug("Data cached with key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting data in Redis for key: {Key}", key);
                // Don't throw - caching should be fail-safe
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken);
                _logger.LogDebug("Cache removed for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing data from Redis for key: {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await _cache.GetAsync(key, cancellationToken);
                return data != null && data.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence in Redis for key: {Key}", key);
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
        {
            // Note: StackExchange.Redis doesn't support pattern matching directly
            // In production, you might want to use Redis commands directly
            _logger.LogWarning("Pattern matching not fully implemented. Consider using SCAN command directly.");
            return new List<string>();
        }

        public async Task ClearAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // In production, you would use FLUSHDB command
                // This is a simplified version
                _logger.LogInformation("Clearing all cache (not implemented in this simplified version)");
                // Note: _cache doesn't have a ClearAll method
                // You would need to use IConnectionMultiplexer for this
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing Redis cache");
            }
        }
        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            try
            {
                // For counters, we need to use Redis string operations
                // This is simplified - in production use Redis INCR command
                var cachedValue = await GetAsync<long>(key);
                var current = cachedValue != 0 ? cachedValue : 0;
                var newValue = current + value;

                await SetAsync(key, newValue, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(_redisSettings.SlidingExpirationMinutes)
                });

                return newValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing value in Redis for key: {Key}", key);
                return 0;
            }
        }

        public async Task<long> DecrementAsync(string key, long value = 1)
        {
            try
            {
                var cachedValue = await GetAsync<long>(key);
                var current = cachedValue != 0 ? cachedValue : 0;
                var newValue = Math.Max(0, current - value); // Don't go below 0

                await SetAsync(key, newValue, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(_redisSettings.SlidingExpirationMinutes)
                });

                return newValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrementing value in Redis for key: {Key}", key);
                return 0;
            }
        }



        // Helper method to generate cache keys
        public static class CacheKeys
        {
            public static string Product(int id) => $"product:{id}";
            public static string ProductsAll => "products:all";
            public static string ProductsByCategory(string category) => $"products:category:{category}";
            public static string ProductsActive => "products:active";
            public static string ProductStock(int productId) => $"product:stock:{productId}";
            public static string Order(int id) => $"order:{id}";
            public static string UserSession(string userId) => $"session:{userId}";
            public static string ApiStats => "api:stats";
        }
    }
}
