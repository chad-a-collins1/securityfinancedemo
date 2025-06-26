using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HighThroughputApi.Helpers
{
    public static class RedisCacheHelper
    {

        public static async Task<T?> GetFromCacheAsync<T>(IDistributedCache cache, string key, ILogger? logger = null, CancellationToken ct = default)
        {
            var cached = await cache.GetStringAsync(key, ct);
            logger?.LogInformation("Redis {HitMiss} {Key}", cached is null ? "MISS" : "HIT", key);
            return cached is null ? default : JsonSerializer.Deserialize<T>(cached);
        }

        public static Task SetCacheAsync<T>(IDistributedCache cache,string key,T value,TimeSpan ttl,ILogger? logger = null,CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(value);

            logger?.LogInformation("Redis SET {Key} (TTL: {TTL}s)", key, ttl.TotalSeconds);

            return cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            }, ct);
        }

        public static Task RemoveCacheAsync(IDistributedCache cache, string key,
                                             CancellationToken ct = default)
            => cache.RemoveAsync(key, ct);

    }
}
