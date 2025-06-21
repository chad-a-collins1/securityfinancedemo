using HighThroughputApi.Interfaces;
using HighThroughputApi.Services;
using HighThroughputApi.Models;
using Microsoft.EntityFrameworkCore;
using HighThroughputApi.Models.Dtos;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace HighThroughputApi.Repositories
{
    public class OrderRepository : BaseRepository<Order>, IOrderRepository
    {
        readonly ItemService _itemService;
        readonly IOrderItemRepository _orderItemRepository;
        
        public OrderRepository(AppDbContext context, ItemService itemService, IOrderItemRepository orderItemRepository)
            : base(context) 
        { 
           _itemService = itemService;
            _orderItemRepository = orderItemRepository; 
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId) =>
            await _dbSet
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.OrderItems)
                .ToListAsync();

        public async Task<Order> GetOrderByOrderIdAsync(int orderId) =>
                        await _dbSet
                .Where(o => o.Id == orderId)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync();

        public async Task<Order> CreateNewOrder(CreateOrderDto dto)
        {
            var order = new Order
            {
                CustomerId = dto.CustomerId,
                OrderItems = dto.OrderItems.Select(i => new OrderItem
                {
                    ItemId = i.ItemId,
                    Quantity = i.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            var success = await _context.SaveChangesAsync();
            if (success > 0)
            {
                foreach (var item in order.OrderItems)
                {
                    await _itemService.PurchaseItemAsync(item.ItemId, item.Quantity);
                }
            }

            return order;

        }
        public async Task<Order>? UpdateOrderAsync(int OrderId, UpdateOrderDto updatedOrderDto)
        {
            var existingOrder = await GetByIdAsync(OrderId);
            if (existingOrder != null)
            {
                try
                {
                    //Add or Subtract Stock
                    foreach (var updateOrderItem in updatedOrderDto.OrderItems)
                    {
                        var existing = existingOrder.OrderItems.Where(i => i.ItemId == updateOrderItem.ItemId).FirstOrDefault();
                        var id = updateOrderItem.ItemId;
                        var quantityDelta = 0;

                        if (updateOrderItem.Quantity > existing.Quantity)
                        {
                            quantityDelta = updateOrderItem.Quantity - existing.Quantity;
                            await _itemService.PurchaseItemAsync(id, quantityDelta); //subtract from the stock
                        }
                        else if (updateOrderItem.Quantity < existing.Quantity)
                        {
                            quantityDelta = existing.Quantity - updateOrderItem.Quantity;
                            await _itemService.ReshelfItemAsync(id, quantityDelta); //add back to the inventory
                        }

                        var modifiedItem = await _orderItemRepository.GetByItemIdAsync(updateOrderItem.ItemId);
                        if (modifiedItem != null)
                        {
                            modifiedItem.Quantity = updateOrderItem.Quantity;
                            _orderItemRepository.Update(modifiedItem);
                            await _orderItemRepository.SaveChangesAsync();
                        }
                    }
                    _dbSet.Update(existingOrder);
                    await SaveChangesAsync();

                    return await GetOrderByOrderIdAsync(OrderId);
                    
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new DbUpdateConcurrencyException("The order was modified by another user.");
                }
            }

            return null;
        }
    }
}
