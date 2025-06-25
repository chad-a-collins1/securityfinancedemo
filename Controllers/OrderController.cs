using HighThroughputApi.Models;
using HighThroughputApi.Models.Dtos;
using HighThroughputApi.Helpers;
using HighThroughputApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.JsonPatch;
using System;
using HighThroughputApi.Interfaces;
using StackExchange.Redis;


namespace HighThroughputApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IOrderRepository _orderRepository; 
        private readonly ItemService _itemService;

        public OrdersController(AppDbContext context, IOrderRepository orderRepository, ItemService itemService )
        {
            _context = context;
            _itemService = itemService;
            _orderRepository = orderRepository;
        }

        [HttpGet("customer/{id}")]
        public async Task<ActionResult<List<OrderDto>>> GetOrdersByCustomer(int id)
        {

            var orders = await _orderRepository.GetOrdersByCustomerIdAsync(id);

            if (orders == null || !orders.Any())
                return NotFound();

            var orderDtos = orders.Select(order => new OrderDto
            {
                Id = order.Id,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto(oi.Id, oi.ItemId, oi.Quantity, oi.Item.Name)).ToList(),
                Etag = Convert.ToBase64String(order.RowVersion)
            }).ToList();

            return orderDtos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Models.Order>> GetOrder(int id)
        {

            var order = await _orderRepository.GetOrderByOrderIdAsync(id);

            if (order == null)
                return NotFound();

            Response.Headers["ETag"] = order.RowVersion.ToEtag();

            var orderDto = new
            {
                id = order.Id,
                customerId = order.CustomerId,
                orderItems = order.OrderItems.Select(oi => new
                {
                    itemId = oi.ItemId,
                    quantity = oi.Quantity,
                    name = oi.Item.Name
                })
            };

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var customer = await _context.Customers.FindAsync(dto.CustomerId);
            if (customer == null) return BadRequest("Invalid customer ID.");

            var order = await _orderRepository.CreateNewOrder(dto);
            var orderDto = new
            {
                id = order.Id,
                customerId = order.CustomerId,
                orderItems = order.OrderItems.Select(oi => new
                {
                    itemId = oi.ItemId,
                    quantity = oi.Quantity,
                    name = oi.Item.Name
                })
            };

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto);
        }



        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchOrder(int id, [FromBody] UpdateOrderDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var clientVersion = Request.Headers["If-Match"].ToString();  
            var dbVersion = order.RowVersion.ToEtag();
            if (!dbVersion.Equals(clientVersion))
                return StatusCode(StatusCodes.Status412PreconditionFailed, "ETag does not match current version.");

            var updatedOrder = await _orderRepository.UpdateOrderAsync(id, dto);
            var newETag = updatedOrder.RowVersion.ToEtag();
            Response.Headers["ETag"] = newETag;

            return Ok(updatedOrder);

        }


        [HttpPut]
        public async Task<IActionResult> UpdateOrder(int orderid, [FromBody] UpdateOrderDto updatedOrderDto)
        {
            var existingOrder = await _orderRepository.GetOrderByOrderIdAsync(orderid);
            if (existingOrder == null)
                return NotFound();

            var serverETag = existingOrder.RowVersion.ToEtag();
            var clientETag = Request.Headers["If-Match"].ToString();

            if (clientETag != serverETag)
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, "Data has changed.");
            }

            var updatedOrder = await _orderRepository.UpdateOrderAsync(orderid, updatedOrderDto);

            var newETag = updatedOrder.RowVersion.ToEtag();
            Response.Headers["ETag"] = newETag;

            return Ok(updatedOrder);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder([FromHeader(Name = "If-Match")] string ifMatch, int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var current = order.RowVersion.ToEtag();
            if (!Request.Matches(current))
                return StatusCode(StatusCodes.Status412PreconditionFailed);

            foreach (var item in order.OrderItems)
            {            
                await _itemService.ReshelfItemAsync(item.ItemId, item.Quantity);
            }
            order.OrderItems.Clear();

            _context.Orders.Remove(order);
            var success = await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}