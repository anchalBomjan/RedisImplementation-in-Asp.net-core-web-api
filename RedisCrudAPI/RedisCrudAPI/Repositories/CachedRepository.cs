using Microsoft.EntityFrameworkCore;
using RedisCrudAPI.Data;
using RedisCrudAPI.Services;
using System.Linq.Expressions;

namespace RedisCrudAPI.Repositories
{
    public class CachedRepository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly IRedisService _redisService;
        private readonly ILogger<CachedRepository<T>> _logger;
        private readonly DbSet<T> _dbSet;
        private readonly string _entityName;

        public CachedRepository(
            AppDbContext context,
            IRedisService redisService,
            ILogger<CachedRepository<T>> logger)
        {
            _context = context;
            _redisService = redisService;
            _logger = logger;
            _dbSet = context.Set<T>();
            _entityName = typeof(T).Name.ToLower();
        }

        private string CacheKey(int id) => $"{_entityName}:{id}";
        private string AllCacheKey => $"{_entityName}s:all";

        public async Task<T?> GetByIdAsync(int id)
        {
            var cacheKey = CacheKey(id);

            // Try cache first
            var cachedEntity = await _redisService.GetAsync<T>(cacheKey);
            if (cachedEntity != null)
            {
                _logger.LogInformation("Cache hit for {EntityName} ID: {Id}", _entityName, id);
                return cachedEntity;
            }

            _logger.LogInformation("Cache miss for {EntityName} ID: {Id}", _entityName, id);

            // Get from database
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
            {
                return null;
            }

            // Cache the result
            await _redisService.SetAsync(cacheKey, entity);

            return entity;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            var cacheKey = AllCacheKey;

            // Try cache first
            var cachedEntities = await _redisService.GetAsync<List<T>>(cacheKey);
            if (cachedEntities != null)
            {
                _logger.LogInformation("Cache hit for all {EntityName}s", _entityName);
                return cachedEntities;
            }

            _logger.LogInformation("Cache miss for all {EntityName}s", _entityName);

            // Get from database
            var entities = await _dbSet.AsNoTracking().ToListAsync();

            // Cache the results
            await _redisService.SetAsync(cacheKey, entities);

            return entities;
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            // For filtered queries, we typically don't cache as they're too diverse
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();

            // Invalidate cache for all entities list
            await _redisService.RemoveAsync(AllCacheKey);

            _logger.LogInformation("Added {EntityName} and invalidated cache", _entityName);

            return entity;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();

            // Invalidate cache for this entity and all entities list
            var idProperty = entity.GetType().GetProperty("Id");
            if (idProperty != null)
            {
                var idValue = idProperty.GetValue(entity);
                if (idValue is int id)
                {
                    await _redisService.RemoveAsync(CacheKey(id));
                }
            }

            await _redisService.RemoveAsync(AllCacheKey);

            _logger.LogInformation("Updated {EntityName} and invalidated cache", _entityName);

            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
            {
                return false;
            }

            // For soft delete, mark as deleted
            var isDeletedProperty = entity.GetType().GetProperty("IsDeleted");
            if (isDeletedProperty != null)
            {
                isDeletedProperty.SetValue(entity, true);
                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }

            await _context.SaveChangesAsync();

            // Invalidate cache
            await _redisService.RemoveAsync(CacheKey(id));
            await _redisService.RemoveAsync(AllCacheKey);

            _logger.LogInformation("Deleted {EntityName} ID: {Id} and invalidated cache", _entityName, id);

            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.FindAsync(id) != null;
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
            {
                return await _dbSet.CountAsync();
            }

            return await _dbSet.CountAsync(predicate);
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
        }
    }
}
