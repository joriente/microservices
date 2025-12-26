using FluentValidation;
using MassTransit;
using Wolverine;
using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.InventoryService.Data;
using ProductOrderingSystem.InventoryService.Models;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.InventoryService.Features.Inventory;

/// <summary>
/// Vertical Slice: Reserve Inventory for Order
/// </summary>
public static class ReserveInventory
{
    // Command
    public record Command(
        Guid OrderId,
        List<ReservationItem> Items);

    public record ReservationItem(Guid ProductId, int Quantity);

    // Result
    public record Result(bool Success, Guid? ReservationId = null, string? ErrorMessage = null);

    // Validator
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId).NotEmpty();
                item.RuleFor(x => x.Quantity).GreaterThan(0);
            });
        }
    }

    // Handler
    public class Handler
    {
        private readonly InventoryDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<Handler> _logger;

        public Handler(
            InventoryDbContext context,
            IPublishEndpoint publishEndpoint,
            ILogger<Handler> logger)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Use execution strategy to handle transactions with retry logic
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                // Use EF Core transaction
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Check and reserve all items
                    foreach (var item in request.Items)
                    {
                        var inventoryItem = await _context.InventoryItems
                            .FirstOrDefaultAsync(x => x.ProductId == item.ProductId, cancellationToken);

                        if (inventoryItem == null)
                        {
                            _logger.LogWarning("Product {ProductId} not found in inventory", item.ProductId);
                            
                            await transaction.RollbackAsync(cancellationToken);
                            
                            // Publish reservation failed event
                            await _publishEndpoint.Publish(new InventoryReservationFailedEvent(
                                request.OrderId,
                                $"Product {item.ProductId} not found in inventory",
                                DateTime.UtcNow
                            ), cancellationToken);
                            
                            return new Result(false, null, $"Product {item.ProductId} not found");
                        }

                        if (inventoryItem.QuantityAvailable < item.Quantity)
                        {
                            _logger.LogWarning("Insufficient inventory for product {ProductId}. Available: {Available}, Requested: {Requested}",
                                item.ProductId, inventoryItem.QuantityAvailable, item.Quantity);
                            
                            await transaction.RollbackAsync(cancellationToken);
                            
                            // Publish reservation failed event
                            await _publishEndpoint.Publish(new InventoryReservationFailedEvent(
                                request.OrderId,
                                $"Insufficient inventory for product {item.ProductId}",
                                DateTime.UtcNow
                            ), cancellationToken);
                            
                            return new Result(false, null, $"Insufficient inventory for product {item.ProductId}");
                        }

                        // Reserve the inventory
                        inventoryItem.Reserve(item.Quantity);

                        // Create reservation record
                        var reservation = new InventoryReservation
                        {
                            Id = Guid.NewGuid(),
                            OrderId = request.OrderId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Status = ReservationStatus.Reserved,
                            CreatedAt = DateTime.UtcNow,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(30) // Reservation expires in 30 minutes
                        };

                        _context.InventoryReservations.Add(reservation);
                    }

                    // Save all changes within the transaction
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Successfully reserved inventory for order {OrderId}", request.OrderId);

                    // Publish inventory reserved event
                    await _publishEndpoint.Publish(new InventoryReservedEvent(
                        request.OrderId,
                        request.Items.Select(i => new ReservedItemDto(
                            i.ProductId,
                            i.Quantity
                        )).ToList(),
                        DateTime.UtcNow
                    ), cancellationToken);

                    // Return the order ID as reservation identifier
                    return new Result(true, request.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reserving inventory for order {OrderId}", request.OrderId);
                    
                    try
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    catch
                    {
                        // Ignore errors during rollback
                    }
                    
                    await _publishEndpoint.Publish(new InventoryReservationFailedEvent(
                        request.OrderId,
                        ex.Message,
                        DateTime.UtcNow
                    ), cancellationToken);
                    
                    return new Result(false, null, ex.Message);
                }
            });
        }
    }

    // Endpoint
    public static IEndpointRouteBuilder MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/inventory/reserve", async (
            Command command,
            IValidator<Command> validator,
            IMessageBus messageBus) =>
        {
            var validationResult = await validator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(new Result(false, null, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }
            
            var result = await messageBus.InvokeAsync<Result>(command);
            
            if (result.Success && result.ReservationId.HasValue)
            {
                return Results.Created($"/api/inventory/reservations/{result.ReservationId}", null);
            }
            
            return Results.BadRequest(new { error = result.ErrorMessage });
        })
        .RequireAuthorization()
        .WithName("ReserveInventory")
        .WithTags("Inventory")
        .Produces(201)
        .Produces(400);

        return app;
    }
}
