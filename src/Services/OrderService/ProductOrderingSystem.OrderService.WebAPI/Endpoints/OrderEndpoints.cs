using ErrorOr;
using MediatR;
using ProductOrderingSystem.OrderService.Application.Commands.Orders;
using ProductOrderingSystem.OrderService.Application.Queries.Orders;
using ProductOrderingSystem.OrderService.Domain.Entities;
using ProductOrderingSystem.Shared.Contracts.Orders;

namespace ProductOrderingSystem.OrderService.WebAPI.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var ordersApi = app.MapGroup("/api/orders")
            .WithTags("Orders");

        // POST /api/orders - Create a new order
        ordersApi.MapPost("/", CreateOrder)
            .WithName("CreateOrder")
            .WithSummary("Create a new order (returns 201 with Location header)")
            .RequireAuthorization()
            .Produces(201) // 201 Created with Location header, no body
            .Produces(400)
            .Produces(404)
            .Produces(500);

        // GET /api/orders/{id} - Get order by ID
        ordersApi.MapGet("/{id}", GetOrderById)
            .WithName("GetOrderById")
            .WithSummary("Get an order by ID")
            .RequireAuthorization()
            .Produces<OrderDto>(200)
            .Produces(404)
            .Produces(500);

        // GET /api/orders - Get orders with optional filters
        ordersApi.MapGet("/", GetOrders)
            .WithName("GetOrders")
            .WithSummary("Get orders with optional filters (pagination metadata in Pagination header)")
            .RequireAuthorization()
            .Produces<IEnumerable<OrderDto>>(200)
            .Produces(400)
            .Produces(500);
    }

    private static async Task<IResult> CreateOrder(CreateOrderRequest request, IMediator mediator, HttpContext httpContext, ILogger<Program> logger)
    {
        // Log incoming request details
        logger.LogInformation(
            "POST /api/orders - Method: {Method}, Path: {Path}, Query: {Query}, RemoteIP: {RemoteIP}, Headers: {@Headers}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Request.QueryString,
            httpContext.Connection.RemoteIpAddress,
            httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
        );
        
        logger.LogInformation(
            "Creating order for CustomerId: {CustomerId}, CustomerEmail: {CustomerEmail}, ItemCount: {ItemCount}",
            request.CustomerId,
            request.CustomerEmail,
            request.Items.Count
        );

        var command = new CreateOrderCommand(
            request.CustomerId,
            request.CustomerEmail,
            request.CustomerName,
            request.Items.Select(item => new CreateOrderItemCommand(
                item.ProductId,
                item.ProductName,
                item.Price,
                item.Quantity
            )).ToList(),
            request.Notes
        );

        var result = await mediator.Send(command);

        return result.Match(
            order =>
            {
                // Follow REST principles: 201 Created with Location header, empty body
                var locationUri = $"/api/orders/{order.Id}";
                httpContext.Response.Headers.Location = locationUri;
                logger.LogInformation(
                    "Order created successfully - OrderId: {OrderId}, Location: {Location}, StatusCode: 201",
                    order.Id,
                    locationUri
                );
                return Results.StatusCode(201); // 201 Created with no body
            },
            errors =>
            {
                logger.LogWarning(
                    "Order creation failed - Errors: {@Errors}",
                    errors.Select(e => new { e.Type, e.Description })
                );
                return MapErrorsToResult(errors);
            }
        );
    }

    private static async Task<IResult> GetOrderById(string id, IMediator mediator)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await mediator.Send(query);

        return result.Match(
            order => Results.Ok(MapToDto(order)),
            errors => MapErrorsToResult(errors)
        );
    }

    private static async Task<IResult> GetOrders(
        string? customerId,
        ProductOrderingSystem.Shared.Contracts.Orders.OrderStatus? status,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        IMediator mediator,
        HttpContext httpContext)
    {
        // Convert contract status to domain status
        ProductOrderingSystem.OrderService.Domain.Entities.OrderStatus? domainStatus = status.HasValue
            ? (ProductOrderingSystem.OrderService.Domain.Entities.OrderStatus)(int)status.Value
            : null;

        var query = new GetOrdersQuery(customerId, domainStatus, startDate, endDate, page, pageSize);
        var result = await mediator.Send(query);

        return result.Match(
            queryResult => 
            {
                // Calculate pagination metadata
                var paginationMetadata = new OrderPaginationMetadata(
                    Page: queryResult.Page,
                    PageSize: queryResult.PageSize,
                    TotalCount: queryResult.TotalCount,
                    TotalPages: queryResult.TotalPages,
                    HasPrevious: queryResult.Page > 1,
                    HasNext: queryResult.Page < queryResult.TotalPages
                );

                // Add pagination metadata to response header as JSON
                httpContext.Response.Headers["X-Pagination"] = System.Text.Json.JsonSerializer.Serialize(paginationMetadata);

                // Return only the orders list in the body
                var orders = queryResult.Orders.Select(MapToDto).ToList();
                return Results.Ok(orders);
            },
            errors => MapErrorsToResult(errors)
        );
    }

    // Helper methods
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

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            order.CustomerName,
            order.Items.Select(item => new OrderItemDto(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.TotalPrice
            )).ToList(),
            order.TotalAmount,
            (ProductOrderingSystem.Shared.Contracts.Orders.OrderStatus)(int)order.Status,
            order.CreatedAt,
            order.UpdatedAt ?? order.CreatedAt,
            order.Notes
        );
    }
}
