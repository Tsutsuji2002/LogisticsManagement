using Logistics.Server.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// In-memory cache service. Will be replaced by Redis in production.
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
            options.SetAbsoluteExpiration(expiration.Value);
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        (_cache as MemoryCache)?.Compact(1.0);
    }
}
