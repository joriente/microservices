using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.InventoryService.Models;

namespace ProductOrderingSystem.InventoryService.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure InventoryItem
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId).IsUnique();
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.QuantityOnHand).IsRequired();
            entity.Property(e => e.QuantityReserved).IsRequired();
            entity.Property(e => e.ReorderLevel).IsRequired();
            entity.Property(e => e.ReorderQuantity).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        // Configure InventoryReservation
        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
