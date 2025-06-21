using HighThroughputApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RedLockNet;
using RedLockNet.SERedis;
using System;

namespace HighThroughputApi.Services;

public class ItemService
{
    private readonly AppDbContext _db;
    private readonly RedLockFactory _redLockFactory;
    private readonly ILogger<ItemService> _logger;

    public ItemService(AppDbContext db, RedLockFactory redLockFactory, ILogger<ItemService> logger)
    {
        _db = db;
        _redLockFactory = redLockFactory;
        _logger = logger;
    }

    public async Task<IActionResult> PurchaseItemAsync(int id, int quantity)
    {
        var resource = $"locks:item:{id}";
        await using var redLock = await _redLockFactory.CreateLockAsync(
            resource,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(200));

        if (!redLock.IsAcquired)
            return new StatusCodeResult(429); //429 = too many requests

        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return new NotFoundResult();
        if (item.Stock < quantity) return new BadRequestObjectResult("Insufficient stock");

        item.Stock -= quantity;

        try
        {
            await _db.SaveChangesAsync();
            return new OkObjectResult(item);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict on item {ItemId}", id);
            return new ConflictObjectResult("Update conflict – please retry");
        }
    }


    public async Task<IActionResult> ReshelfItemAsync(int id, int quantity)
    {
        var resource = $"locks:item:{id}";
        await using var redLock = await _redLockFactory.CreateLockAsync(
            resource,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(200));

        if (!redLock.IsAcquired)
            return new StatusCodeResult(429); //429 = too many requests

        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return new NotFoundResult();

        item.Stock += quantity;

        try
        {
            await _db.SaveChangesAsync();
            return new OkObjectResult(item);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict on item {ItemId}", id);
            return new ConflictObjectResult("Update conflict – please retry");
        }
    }

}
