using HighThroughputApi.Models;
using HighThroughputApi.Models.Dtos;
using HighThroughputApi.Helpers;
using HighThroughputApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.JsonPatch;
using System;
using HighThroughputApi.Interfaces;


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
            var orders = await _context.Orders
                .Where(o => o.CustomerId == id)
                .Include(o => o.OrderItems)
                .ToListAsync();

            if (orders == null || !orders.Any())
                return NotFound();

            var orderDtos = orders.Select(order => new OrderDto
            {
                Id = order.Id,
                OrderItemsDto = order.OrderItems.Select(oi => new OrderItemDto(oi.ItemId, oi.Quantity)).ToList()
                    
            }).ToList();

            return orderDtos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            Response.Headers["ETag"] = order.RowVersion.ToEtag();
            return Ok(order);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var customer = await _context.Customers.FindAsync(dto.CustomerId);
            if (customer == null) return BadRequest("Invalid customer ID.");

            var order = await _orderRepository.CreateNewOrder(dto);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }



        //[HttpPatch("{id}")]
        //public async Task<IActionResult> PatchOrder(int id, [FromHeader(Name = "If-Match")] string ifMatch, [FromBody] UpdateOrderDto dto)
        //{
        //    var order = await _context.Orders
        //        .Include(o => o.OrderItems)
        //        .FirstOrDefaultAsync(o => o.Id == id);

        //    if (order == null)
        //        return NotFound();


        //    var clientVersion = Request.Headers["If-Match"].ToString().Trim('"');
        //    var dbVersion = order.RowVersion.ToEtag();
        //    if (clientVersion != dbVersion)
        //        return StatusCode(StatusCodes.Status412PreconditionFailed, "ETag does not match current version.");


        //    order.OrderItems.Clear();
        //    foreach (var itemDto in dto.OrderItems)
        //    {
        //        order.OrderItems.Add(new OrderItem
        //        {
        //            ItemId = itemDto.ItemId,
        //            Quantity = itemDto.Quantity
        //        });
        //    }

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        return StatusCode(StatusCodes.Status409Conflict, "Concurrency conflict.");
        //    }

        //    return NoContent();
        //}


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