using HighThroughputApi.Interfaces;
using HighThroughputApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HighThroughputApi.Repositories
{
    public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CustomerRepository> _logger;
        private static readonly TimeSpan CustomerTtl = TimeSpan.FromMinutes(120); //cache each customer for 2 hours
        private static string CacheKey_Email(string email) => $"customer:email:{email.ToLowerInvariant()}";

        public CustomerRepository(AppDbContext context,
            IDistributedCache cache,
            ILogger<CustomerRepository> logger)          
            : base(context)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            var key = CacheKey_Email(email);

            var cached = await _cache.GetStringAsync(key, ct);
            _logger.LogInformation("Redis {HitMiss} {Key}",
                cached is null ? "MISS" : "HIT", key);

            if (cached is not null)
                return JsonSerializer.Deserialize<Customer>(cached);

            var customer = await _dbSet.FirstOrDefaultAsync(c => c.Email == email, ct);
            if (customer is null) return null;

            await _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(customer),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CustomerTtl
                }, ct);

            return customer;
        }



    }
}
