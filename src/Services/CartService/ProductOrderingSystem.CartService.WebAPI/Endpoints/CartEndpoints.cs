using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductOrderingSystem.CartService.Application.Commands.Carts;
using ProductOrderingSystem.CartService.Application.Queries.Carts;
using ProductOrderingSystem.Shared.Contracts.Carts;
using System.Security.Claims;

namespace ProductOrderingSystem.CartService.WebAPI.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/carts")
            .RequireAuthorization()
            .WithTags("Carts");

        // Get current user's cart
        group.MapGet("/me", async (
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var query = new GetCartByCustomerIdQuery(userId);
            var result = await mediator.Send(query);

            return result.Match(
                value => Results.Ok(value),
                errors => Results.NotFound(new { error = errors[0].Code, description = errors[0].Description })
            );
        });

        // Get cart by ID
        group.MapGet("/{id}", async (
            string id,
            IMediator mediator) =>
        {
            var query = new GetCartByIdQuery(id);
            var result = await mediator.Send(query);

            return result.Match(
                value => Results.Ok(value),
                errors => Results.NotFound(new { error = errors[0].Code, description = errors[0].Description })
            );
        });

        // Get cart by customer ID
        group.MapGet("/customer/{customerId}", async (
            string customerId,
            IMediator mediator) =>
        {
            var query = new GetCartByCustomerIdQuery(customerId);
            var result = await mediator.Send(query);

            return result.Match(
                value => Results.Ok(value),
                errors => Results.NotFound(new { error = errors[0].Code, description = errors[0].Description })
            );
        });

        // Create cart (or get existing)
        group.MapPost("/", async (
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                return Results.Unauthorized();
            }

            var command = new CreateCartCommand(userId, userEmail);
            var result = await mediator.Send(command);

            return result.Match(
                value => Results.Ok(value),
                errors => Results.BadRequest(new { error = errors[0].Code, description = errors[0].Description })
            );
        });

        // Add item to cart
        group.MapPost("/{cartId}/items", async (
            string cartId,
            [FromBody] AddItemToCartRequest request,
            IMediator mediator) =>
        {
            var command = new AddItemToCartCommand(
                cartId,
                request.ProductId,
                request.ProductName,
                request.Price,
                request.Quantity
            );

            var result = await mediator.Send(command);

            return result.Match(
                value => Results.NoContent(),
                errors => errors[0].Type switch
                {
                    ErrorOr.ErrorType.NotFound => Results.NotFound(new { error = errors[0].Code, description = errors[0].Description }),
                    ErrorOr.ErrorType.Validation => Results.BadRequest(new { error = errors[0].Code, description = errors[0].Description }),
                    _ => Results.BadRequest(new { error = errors[0].Code, description = errors[0].Description })
                }
            );
        });

        // Add item to current user's cart (creates cart if doesn't exist)
        group.MapPost("/items", async (
            [FromBody] AddItemToCartRequest request,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                return Results.Unauthorized();
            }

            // Get or create cart for user
            var cartQuery = new GetCartByCustomerIdQuery(userId);
            var cartResult = await mediator.Send(cartQuery);

            string cartId;
            if (cartResult.IsError)
            {
                // Cart doesn't exist, create it
                var createCommand = new CreateCartCommand(userId, userEmail);
                var createResult = await mediator.Send(createCommand);
                
                if (createResult.IsError)
                {
                    return Results.BadRequest(new { error = createResult.Errors[0].Code, description = createResult.Errors[0].Description });
                }
                
                cartId = createResult.Value.CartId;
            }
            else
            {
                cartId = cartResult.Value.Id;
            }

            // Add item to cart
            var command = new AddItemToCartCommand(
                cartId,
                request.ProductId,
                request.ProductName,
                request.Price,
                request.Quantity
            );

            var result = await mediator.Send(command);

            return result.Match(
                value => {
                    // Return the updated cart
                    var updatedCartQuery = new GetCartByIdQuery(cartId);
                    var updatedCart = mediator.Send(updatedCartQuery).Result;
                    return updatedCart.Match(
                        cart => Results.Ok(cart),
                        errors => Results.Ok(value)
                    );
                },
                errors => errors[0].Type switch
                {
                    ErrorOr.ErrorType.NotFound => Results.NotFound(new { error = errors[0].Code, description = errors[0].Description }),
                    ErrorOr.ErrorType.Validation => Results.BadRequest(new { error = errors[0].Code, description = errors[0].Description }),
                    _ => Results.BadRequest(new { error = errors[0].Code, description = errors[0].Description })
                }
            );
        });

        // Update item quantity
        group.MapPut("/{cartId}/items/{productId}", async (
            string cartId,
            string productId,
            [FromBody] UpdateItemQuantityRequest request,
            IMediator mediator) =>
        {
            var command = new UpdateItemQuantityCommand(cartId, productId, request.Quantity);
            var result = await mediator.Send(command);

            return result.Match(
                value => Results.NoContent(),
                errors => errors[0].Type switch
                {
                    ErrorOr.ErrorType.NotFound => Results.NotFound(new { error = errors[0].Code, description = errors[0].Description }),
                    ErrorOr.ErrorType.Validation => Results.BadRequest(new { error = errors[0].Code, description = errors[0].Description }),
                    _ => Results.BadRequest(new { error = errors[0].Code, description = errors[0].Description })
                }
            );
        });

        // Remove item from cart
        group.MapDelete("/{cartId}/items/{productId}", async (
            string cartId,
            string productId,
            IMediator mediator) =>
        {
            var command = new RemoveItemFromCartCommand(cartId, productId);
            var result = await mediator.Send(command);

            return result.Match(
                value => Results.NoContent(),
                errors => Results.NotFound(new { error = errors[0].Code, description = errors[0].Description })
            );
        });

        // Clear cart
        group.MapDelete("/{cartId}", async (
            string cartId,
            IMediator mediator) =>
        {
            var command = new ClearCartCommand(cartId);
            var result = await mediator.Send(command);

            return result.Match(
                value => Results.NoContent(),
                errors => Results.NotFound(new { error = errors[0].Code, description = errors[0].Description })
            );
        });
    }
}
