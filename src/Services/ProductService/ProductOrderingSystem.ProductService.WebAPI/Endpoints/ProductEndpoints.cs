using ErrorOr;
using MediatR;
using ProductOrderingSystem.ProductService.Application.Commands.Products;
using ProductOrderingSystem.ProductService.Application.Queries.Products;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.Shared.Contracts.Products;

namespace ProductOrderingSystem.ProductService.WebAPI.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var productsApi = app.MapGroup("/api/products")
            .WithTags("Products")
            .WithOpenApi();

        // GET /api/products - Search products with filters
        productsApi.MapGet("/", SearchProducts)
            .WithName("SearchProducts")
            .WithSummary("Search products with optional filters (pagination metadata in Pagination header)")
            .Produces<IEnumerable<ProductDto>>(200)
            .Produces(500);

        // GET /api/products/{id} - Get product by ID
        productsApi.MapGet("/{id}", GetProductById)
            .WithName("GetProductById")
            .WithSummary("Get a product by ID")
            .Produces<ProductDto>(200)
            .Produces(404)
            .Produces(500);

        // POST /api/products - Create a new product
        productsApi.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .WithSummary("Create a new product (returns 201 with Location header)")
            .RequireAuthorization()
            .Produces(201) // 201 Created with Location header, no body
            .Produces(400)
            .Produces(500);

        // PUT /api/products/{id} - Update a product
        productsApi.MapPut("/{id}", UpdateProduct)
            .WithName("UpdateProduct")
            .WithSummary("Update an existing product")
            .RequireAuthorization()
            .Produces<ProductDto>(200)
            .Produces(400)
            .Produces(404)
            .Produces(500);

        // DELETE /api/products/{id} - Delete a product
        productsApi.MapDelete("/{id}", DeleteProduct)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .RequireAuthorization()
            .Produces(204)
            .Produces(404)
            .Produces(500);
    }

    private static async Task<IResult> SearchProducts(
        [AsParameters] ProductSearchRequest request,
        IMediator mediator,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation(
                "SearchProducts called - SearchTerm: {SearchTerm}, Category: {Category}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}, Page: {Page}, PageSize: {PageSize}, RemoteIP: {RemoteIP}",
                request.SearchTerm ?? "null",
                request.Category ?? "null",
                request.MinPrice?.ToString() ?? "null",
                request.MaxPrice?.ToString() ?? "null",
                request.Page,
                request.PageSize,
                httpContext.Connection.RemoteIpAddress
            );

            var query = new SearchProductsQuery(
                request.SearchTerm,
                request.Category,
                request.MinPrice,
                request.MaxPrice,
                request.Page,
                request.PageSize
            );

            var result = await mediator.Send(query);

            logger.LogInformation(
                "SearchProducts completed - Found {TotalCount} products, returning page {Page}/{TotalPages}",
                result.TotalCount,
                result.Page,
                (int)Math.Ceiling(result.TotalCount / (double)result.PageSize)
            );

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize);
            var paginationMetadata = new PaginationMetadata(
                Page: result.Page,
                PageSize: result.PageSize,
                TotalCount: result.TotalCount,
                TotalPages: totalPages,
                HasPrevious: result.Page > 1,
                HasNext: result.Page < totalPages
            );

            // Add pagination metadata to response header as JSON
            httpContext.Response.Headers["X-Pagination"] = System.Text.Json.JsonSerializer.Serialize(paginationMetadata);

            // Return only the products list in the body
            var products = result.Products.Select(MapToDto).ToList();
            return Results.Ok(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SearchProducts - SearchTerm: {SearchTerm}, Page: {Page}", 
                request.SearchTerm ?? "null", 
                request.Page);
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> GetProductById(string id, IMediator mediator, ILogger<Program> logger, HttpContext httpContext)
    {
        try
        {
            logger.LogInformation("GetProductById called - ProductId: {ProductId}, RemoteIP: {RemoteIP}", 
                id, 
                httpContext.Connection.RemoteIpAddress);

            var query = new GetProductByIdQuery(id);
            var product = await mediator.Send(query);

            if (product == null)
            {
                logger.LogWarning("GetProductById - Product not found: {ProductId}", id);
                return Results.NotFound(new { message = "Product not found" });
            }

            logger.LogInformation("GetProductById completed - ProductId: {ProductId}, ProductName: {ProductName}", 
                id, 
                product.Name);

            return Results.Ok(MapToDto(product));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetProductById - ProductId: {ProductId}", id);
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> CreateProduct(CreateProductRequest request, IMediator mediator, HttpContext httpContext, ILogger<Program> logger)
    {
        logger.LogInformation(
            "CreateProduct called - Name: {Name}, Price: {Price}, Stock: {Stock}, Category: {Category}, RemoteIP: {RemoteIP}",
            request.Name,
            request.Price,
            request.StockQuantity,
            request.Category,
            httpContext.Connection.RemoteIpAddress
        );

        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.Category,
            request.ImageUrl
        );

        var result = await mediator.Send(command);

        return result.Match(
            product =>
            {
                logger.LogInformation("CreateProduct completed - ProductId: {ProductId}, Name: {Name}", 
                    product.Id, 
                    product.Name);

                // Follow REST principles: 201 Created with Location header, empty body
                var locationUri = $"/api/products/{product.Id}";
                httpContext.Response.Headers.Location = locationUri;
                return Results.StatusCode(201); // 201 Created with no body
            },
            errors =>
            {
                logger.LogWarning("CreateProduct failed - Name: {Name}, Errors: {Errors}", 
                    request.Name, 
                    string.Join(", ", errors.Select(e => e.Description)));
                return MapErrorsToResult(errors);
            }
        );
    }

    private static async Task<IResult> UpdateProduct(string id, UpdateProductRequest request, IMediator mediator, ILogger<Program> logger, HttpContext httpContext)
    {
        logger.LogInformation(
            "UpdateProduct called - ProductId: {ProductId}, Name: {Name}, Price: {Price}, RemoteIP: {RemoteIP}",
            id,
            request.Name,
            request.Price,
            httpContext.Connection.RemoteIpAddress
        );

        if (id != request.Id)
        {
            logger.LogWarning("UpdateProduct - ID mismatch: URL={UrlId}, Body={BodyId}", id, request.Id);
            return Results.BadRequest(new { message = "ID mismatch" });
        }

        var command = new UpdateProductCommand(
            request.Id,
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.Category,
            request.ImageUrl
        );

        var result = await mediator.Send(command);

        return result.Match(
            product =>
            {
                logger.LogInformation("UpdateProduct completed - ProductId: {ProductId}, Name: {Name}", 
                    product.Id, 
                    product.Name);
                return Results.Ok(MapToDto(product));
            },
            errors =>
            {
                logger.LogWarning("UpdateProduct failed - ProductId: {ProductId}, Errors: {Errors}", 
                    id, 
                    string.Join(", ", errors.Select(e => e.Description)));
                return MapErrorsToResult(errors);
            }
        );
    }

    private static async Task<IResult> DeleteProduct(string id, IMediator mediator, ILogger<Program> logger, HttpContext httpContext)
    {
        try
        {
            logger.LogInformation("DeleteProduct called - ProductId: {ProductId}, RemoteIP: {RemoteIP}", 
                id, 
                httpContext.Connection.RemoteIpAddress);

            var command = new DeleteProductCommand(id);
            await mediator.Send(command);
            
            logger.LogInformation("DeleteProduct completed - ProductId: {ProductId}", id);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
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

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto(
            product.Id.ToString(),
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.Category,
            product.ImageUrl,
            product.CreatedAt,
            product.UpdatedAt
        );
    }
}
