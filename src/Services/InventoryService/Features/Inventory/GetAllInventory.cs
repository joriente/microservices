using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.InventoryService.Data;
using ProductOrderingSystem.InventoryService.Models;

namespace ProductOrderingSystem.InventoryService.Features.Inventory;

/// <summary>
/// Vertical Slice: Get All Inventory Items
/// </summary>
public static class GetAllInventory
{
    // Query
    public record Query : IRequest<Response>;

    // Response
    public record Response(List<InventoryItemDto> Items);

    public record InventoryItemDto(
        Guid Id,
        Guid ProductId,
        string ProductName,
        int Quantity,
        int ReservedQuantity,
        int AvailableQuantity,
        DateTime LastUpdated);

    // Handler
    public class Handler : IRequestHandler<Query, Response>
    {
        private readonly InventoryDbContext _context;

        public Handler(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var items = await _context.InventoryItems
                .OrderBy(x => x.ProductName)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(item => new InventoryItemDto(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.QuantityOnHand,
                item.QuantityReserved,
                item.QuantityAvailable,
                item.UpdatedAt
            )).ToList();

            return new Response(dtos);
        }
    }

    // Endpoint
    public static IEndpointRouteBuilder MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inventory", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new Query());
            return Results.Ok(result.Items);
        })
        .RequireAuthorization()
        .WithName("GetAllInventory")
        .WithTags("Inventory")
        .Produces<List<InventoryItemDto>>();

        return app;
    }
}
