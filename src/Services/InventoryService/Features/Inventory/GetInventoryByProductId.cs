using ErrorOr;
using Wolverine;
using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.InventoryService.Data;
using ProductOrderingSystem.InventoryService.Models;

namespace ProductOrderingSystem.InventoryService.Features.Inventory;

/// <summary>
/// Vertical Slice: Get Inventory by Product ID
/// </summary>
public static class GetInventoryByProductId
{
    // Query
    public record Query(Guid ProductId);

    // Response
    public record Response(
        Guid Id,
        Guid ProductId,
        string ProductName,
        int QuantityOnHand,
        int QuantityReserved,
        int AvailableQuantity, // Match the test expectation
        int ReorderLevel,
        bool IsLowStock,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    // Handler
    public class Handler
    {
        private readonly InventoryDbContext _context;

        public Handler(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<ErrorOr<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(x => x.ProductId == request.ProductId, cancellationToken);

            if (item == null)
                return Error.NotFound("InventoryItem.NotFound", $"Inventory item not found for product {request.ProductId}");

            return new Response(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.QuantityOnHand,
                item.QuantityReserved,
                item.QuantityAvailable, // Maps to AvailableQuantity in JSON
                item.ReorderLevel,
                item.IsLowStock(),
                item.CreatedAt,
                item.UpdatedAt);
        }
    }

    // Endpoint
    public static IEndpointRouteBuilder MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inventory/{productId:guid}", async (
            Guid productId,
            IMessageBus messageBus) =>
        {
            var result = await messageBus.InvokeAsync<ErrorOr<Response>>(new Query(productId));
            return result.Match(
                success => Results.Ok(success),
                errors => MapErrorsToResult(errors)
            );
        })
        .WithName("GetInventoryByProductId")
        .WithTags("Inventory")
        .Produces<Response>()
        .Produces(404);

        return app;
    }

    private static IResult MapErrorsToResult(List<Error> errors)
    {
        var firstError = errors.First();

        return firstError.Type switch
        {
            ErrorType.Validation => Results.BadRequest(new { message = firstError.Description, errors = errors.Select(e => e.Description) }),
            ErrorType.NotFound => Results.NotFound(new { message = firstError.Description }),
            ErrorType.Conflict => Results.Conflict(new { message = firstError.Description }),
            ErrorType.Unauthorized => Results.Unauthorized(),
            ErrorType.Forbidden => Results.Forbid(),
            _ => Results.Problem(firstError.Description, statusCode: 500)
        };
    }
}
