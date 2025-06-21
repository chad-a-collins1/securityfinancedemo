using HighThroughputApi.Models;
using HighThroughputApi.Models.Dtos;

namespace HighThroughputApi.Interfaces
{
    public interface IOrderRepository : IBaseRepository<Order>
    {
        Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId);
        Task<Order> GetOrderByOrderIdAsync(int orderId);
        Task<Order> UpdateOrderAsync(int orderid, UpdateOrderDto order);
        Task<Order> CreateNewOrder(CreateOrderDto dto);
    }
}
