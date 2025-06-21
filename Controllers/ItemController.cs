using HighThroughputApi.Models;
using HighThroughputApi.Models.Dtos;
using HighThroughputApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace HighThroughputApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ItemController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ItemService _itemService;

    public ItemController(AppDbContext db, ItemService itemService)
    {
        _db = db;
        _itemService = itemService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateItemDto dto)
    {
        var item = new Item { Name = dto.Name, Stock = dto.Stock };
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Items.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _db.Items.ToListAsync();
        return items is null ? NotFound() : Ok(items);
    }

    [HttpPost("{id:int}/purchase")]
    public async Task<IActionResult> Purchase(int id, [FromQuery] int quantity)
    {
        var result = await _itemService.PurchaseItemAsync(id, quantity);
        return result;
    }
}
