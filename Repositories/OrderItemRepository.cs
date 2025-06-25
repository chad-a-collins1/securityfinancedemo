using HighThroughputApi.Interfaces;
using HighThroughputApi.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace HighThroughputApi.Repositories
{
    public class OrderItemRepository : BaseRepository<OrderItem>, IOrderItemRepository
    {
        public OrderItemRepository(AppDbContext context) : base(context) {}

        public async Task<OrderItem> GetByItemIdAsync(int orderitemId) =>
        await _dbSet
                .Where(o => o.Id == orderitemId)
                .FirstOrDefaultAsync();

    }
}
