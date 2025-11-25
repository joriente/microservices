using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.InventoryService.Data;
using ProductOrderingSystem.InventoryService.Models;

namespace ProductOrderingSystem.InventoryService.Features.Inventory;

/// <summary>
/// Vertical Slice: Adjust Inventory (Add or Remove Stock)
/// </summary>
public static class AdjustInventory
{
    // Command
    public record Command(
        Guid ProductId,
        int Quantity, // Positive for adding, negative for removing
        string Reason) : IRequest<Response?>;

    // Response
    public record Response(
        Guid Id,
        Guid ProductId,
        string ProductName,
        int Quantity,
        int ReservedQuantity,
        int AvailableQuantity,
        DateTime LastUpdated);

    // Validator
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Product ID is required");

            RuleFor(x => x.Quantity)
                .NotEqual(0).WithMessage("Quantity adjustment must be non-zero");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required")
                .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");
        }
    }

    // Handler
    public class Handler : IRequestHandler<Command, Response?>
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<Handler> _logger;

        public Handler(InventoryDbContext context, ILogger<Handler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Response?> Handle(Command request, CancellationToken cancellationToken)
        {
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(x => x.ProductId == request.ProductId, cancellationToken);

            if (item == null)
            {
                _logger.LogWarning("Inventory item not found for product {ProductId}", request.ProductId);
                return null;
            }

            // Adjust the inventory
            if (request.Quantity > 0)
            {
                // Adding stock (restocking)
                item.Restock(request.Quantity);
                _logger.LogInformation(
                    "Added {Quantity} units to inventory for product {ProductId}. Reason: {Reason}",
                    request.Quantity, request.ProductId, request.Reason);
            }
            else
            {
                // Removing stock
                var quantityToRemove = Math.Abs(request.Quantity);
                
                // Check if we have enough available quantity
                if (quantityToRemove > item.QuantityAvailable)
                {
                    throw new InvalidOperationException(
                        $"Insufficient available inventory. Available: {item.QuantityAvailable}, Requested to remove: {quantityToRemove}");
                }

                item.QuantityOnHand -= quantityToRemove;
                item.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogInformation(
                    "Removed {Quantity} units from inventory for product {ProductId}. Reason: {Reason}",
                    quantityToRemove, request.ProductId, request.Reason);
            }

            // Save the updated item
            await _context.SaveChangesAsync(cancellationToken);

            return new Response(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.QuantityOnHand,
                item.QuantityReserved,
                item.QuantityAvailable,
                item.UpdatedAt);
        }
    }

    // Endpoint
    public static IEndpointRouteBuilder MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/inventory/adjust", async (
            Command command,
            IMediator mediator,
            IValidator<Command> validator) =>
        {
            var validationResult = await validator.ValidateAsync(command);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            try
            {
                var result = await mediator.Send(command);
                return result is not null 
                    ? Results.Ok(result) 
                    : Results.NotFound(new { message = "Inventory item not found" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithName("AdjustInventory")
        .WithTags("Inventory")
        .Produces<Response>()
        .Produces(404)
        .Produces(400)
        .ProducesValidationProblem();

        return app;
    }
}
