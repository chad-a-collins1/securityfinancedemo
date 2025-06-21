using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HighThroughputApi.Models;

public class AppDbContext : DbContext
{
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>()
            .Property(i => i.RowVersion)
            .IsRowVersion()
            .IsRequired(false);

        modelBuilder.Entity<Order>()
            .Property(i => i.RowVersion)
            .IsRowVersion()
            .IsRequired(false);

        modelBuilder.Entity<Customer>()
            .Property(i => i.RowVersion)
            .IsRowVersion()
            .IsRequired(false);

        modelBuilder.Entity<OrderItem>()
            .Property(i => i.RowVersion)
            .IsRowVersion()
            .IsRequired(false);
    }
}
