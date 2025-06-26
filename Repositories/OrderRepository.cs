using HighThroughputApi.Interfaces;
using HighThroughputApi.Services;
using HighThroughputApi.Models;
using HighThroughputApi.Helpers;
using Microsoft.EntityFrameworkCore;
using HighThroughputApi.Models.Dtos;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

namespace HighThroughputApi.Repositories
{
    public class OrderRepository : BaseRepository<Models.Order>, IOrderRepository
    {
        readonly ItemService _itemService;
        readonly IOrderItemRepository _orderItemRepository;
        private readonly IDistributedCache _cache;
        private static readonly TimeSpan OrderTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan CustomerListTtl = TimeSpan.FromMinutes(3);
        private static string CacheKey_Order(int orderId) => $"order:{orderId}";
        private static string CacheKey_Customer(int customerId) => $"customer:{customerId}:orders";

        public OrderRepository(
            AppDbContext context,
            ItemService itemService,
            IOrderItemRepository orderItemRepository,
            IDistributedCache cache)           
            : base(context)
        {
            _itemService = itemService;
            _orderItemRepository = orderItemRepository;
            _cache = cache;
        }


        public async Task<IEnumerable<Models.Order>> GetOrdersByCustomerIdAsync(int customerId)
        {
            var cached = await RedisCacheHelper.GetFromCacheAsync<List<Models.Order>>( _cache, CacheKey_Customer(customerId));
            if (cached is not null) return cached;

            var orders = await _dbSet
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .ToListAsync();

            await RedisCacheHelper.SetCacheAsync(_cache, CacheKey_Customer(customerId),orders, CustomerListTtl);
            return orders;
        }


        public async Task<Models.Order> GetOrderByOrderIdAsync(int orderId)
        {
            var cached = await RedisCacheHelper.GetFromCacheAsync<Models.Order>(_cache, CacheKey_Order(orderId));
            if (cached is not null) return cached;

            var order = await _dbSet
                    .Where(o => o.Id == orderId)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                    .FirstOrDefaultAsync();

            if (order is not null)
                await RedisCacheHelper.SetCacheAsync(_cache, CacheKey_Order(orderId), order, OrderTtl);

            return order;

        }


        public async Task<Models.Order> CreateNewOrder(CreateOrderDto dto)
        {
            var order = new Models.Order
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
                    await _itemService.PurchaseItemAsync(item.ItemId, item.Quantity);
            }

            // redis cache write‑through for single order 
            await RedisCacheHelper.SetCacheAsync(_cache, CacheKey_Order(order.Id), order, OrderTtl);
            // this is a lazy re-populat to invalidate the list for the customer who requests it
            await RedisCacheHelper.RemoveCacheAsync(_cache, CacheKey_Customer(dto.CustomerId));

            return order;
        }




        public async Task<Models.Order>? UpdateOrderAsync(int OrderId, UpdateOrderDto updatedOrderDto)
        {
            var existingOrder = await GetByIdAsync(OrderId);
            if (existingOrder != null)
            {
                try
                {
                    //Add or Subtract Stock
                    foreach (var updateOrderItem in updatedOrderDto.Items)
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

                        var modifiedItem = await _orderItemRepository.GetByItemIdAsync(updateOrderItem.OrderItemId);
                        if (modifiedItem != null)
                        {
                            modifiedItem.Quantity = updateOrderItem.Quantity;
                            _orderItemRepository.Update(modifiedItem);
                            await _orderItemRepository.SaveChangesAsync();
                        }
                    }
                    _dbSet.Update(existingOrder);
                    await _context.SaveChangesAsync();

                    // refresh single order cache
                    var fresh = await GetOrderByOrderIdAsync(OrderId);
                    if (fresh is not null)
                        await RedisCacheHelper.SetCacheAsync(_cache, CacheKey_Order(OrderId), fresh, OrderTtl);

                    //invalidate list for that customer
                    await RedisCacheHelper.RemoveCacheAsync(_cache, CacheKey_Customer(fresh!.CustomerId));

                    return fresh;
                    
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
