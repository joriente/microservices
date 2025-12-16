using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductOrderingSystem.AnalyticsService.Domain.Entities;

namespace ProductOrderingSystem.AnalyticsService.Infrastructure.Data;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
    {
    }

    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<ProductEvent> ProductEvents => Set<ProductEvent>();
    public DbSet<InventoryEvent> InventoryEvents => Set<InventoryEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.EventTimestamp);
            entity.HasIndex(e => new { e.CustomerId, e.EventTimestamp });
        });

        modelBuilder.Entity<PaymentEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.EventTimestamp);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProductEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.EventTimestamp);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<InventoryEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.EventTimestamp);
        });
    }
}
