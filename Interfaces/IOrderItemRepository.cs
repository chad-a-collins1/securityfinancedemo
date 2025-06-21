using HighThroughputApi.Models;

namespace HighThroughputApi.Interfaces
{
    public interface IOrderItemRepository : IBaseRepository<OrderItem>
    {
        Task<OrderItem> GetByItemIdAsync(int id);
    }
}
