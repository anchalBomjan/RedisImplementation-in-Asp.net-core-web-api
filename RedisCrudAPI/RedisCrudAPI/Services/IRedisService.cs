using Microsoft.Extensions.Caching.Distributed;

namespace RedisCrudAPI.Services
{
    public interface IRedisService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern);
        Task ClearAllAsync(CancellationToken cancellationToken = default);
        Task<long> IncrementAsync(string key, long value = 1);
        Task<long> DecrementAsync(string key, long value = 1);
    }

}
