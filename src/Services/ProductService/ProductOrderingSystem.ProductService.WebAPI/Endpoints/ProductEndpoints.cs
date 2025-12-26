using ErrorOr;
using Wolverine;
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
            .WithTags("Products");

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

        // POST /api/products/sync-cache - Republish events for all products
        productsApi.MapPost("/sync-cache", SyncProductCache)
            .WithName("SyncProductCache")
            .WithSummary("Republish ProductCreatedEvent for all existing products to populate consumer caches")
            .Produces(200)
            .Produces(500);
    }

    private static async Task<IResult> SearchProducts(
        [AsParameters] ProductSearchRequest request,
        IMessageBus messageBus,
        HttpContext httpContext,
        ILogger<Program> logger)
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

        var result = await messageBus.InvokeAsync<ErrorOr<SearchProductsResult>>(query);

        return result.Match(
            searchResult =>
            {
                logger.LogInformation(
                    "SearchProducts completed - Found {TotalCount} products, returning page {Page}/{TotalPages}",
                    searchResult.TotalCount,
                    searchResult.Page,
                    (int)Math.Ceiling(searchResult.TotalCount / (double)searchResult.PageSize)
                );

                // Calculate pagination metadata
                var totalPages = (int)Math.Ceiling(searchResult.TotalCount / (double)searchResult.PageSize);
                var paginationMetadata = new PaginationMetadata(
                    Page: searchResult.Page,
                    PageSize: searchResult.PageSize,
                    TotalCount: searchResult.TotalCount,
                    TotalPages: totalPages,
                    HasPrevious: searchResult.Page > 1,
                    HasNext: searchResult.Page < totalPages
                );

                // Add pagination metadata to response header as JSON
                httpContext.Response.Headers["X-Pagination"] = System.Text.Json.JsonSerializer.Serialize(paginationMetadata);

                // Return only the products list in the body
                var products = searchResult.Products.Select(MapToDto).ToList();
                return Results.Ok(products);
            },
            errors => MapErrorsToResult(errors)
        );
    }

    private static async Task<IResult> GetProductById(string id, IMessageBus messageBus, ILogger<Program> logger, HttpContext httpContext)
    {
        logger.LogInformation("GetProductById called - ProductId: {ProductId}, RemoteIP: {RemoteIP}", 
            id, 
            httpContext.Connection.RemoteIpAddress);

        var query = new GetProductByIdQuery(id);
        var result = await messageBus.InvokeAsync<ErrorOr<Product>>(query);

        return result.Match(
            product =>
            {
                logger.LogInformation("GetProductById completed - ProductId: {ProductId}, ProductName: {ProductName}", 
                    id, 
                    product.Name);
                return Results.Ok(MapToDto(product));
            },
            errors => MapErrorsToResult(errors)
        );
    }

    private static async Task<IResult> CreateProduct(CreateProductRequest request, IMessageBus messageBus, HttpContext httpContext, ILogger<Program> logger)
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

        var result = await messageBus.InvokeAsync<ErrorOr<Product>>(command);

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
            errors => MapErrorsToResult(errors)
        );
    }

    private static async Task<IResult> UpdateProduct(string id, UpdateProductRequest request, IMessageBus messageBus, ILogger<Program> logger, HttpContext httpContext)
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

        var result = await messageBus.InvokeAsync<ErrorOr<Product>>(command);

        return result.Match(
            product =>
            {
                logger.LogInformation("UpdateProduct completed - ProductId: {ProductId}, Name: {Name}", 
                    product.Id, 
                    product.Name);
                return Results.Ok(MapToDto(product));
            },
            errors => MapErrorsToResult(errors)
        );
    }

    private static async Task<IResult> DeleteProduct(string id, IMessageBus messageBus, ILogger<Program> logger, HttpContext httpContext)
    {
        logger.LogInformation("DeleteProduct called - ProductId: {ProductId}, RemoteIP: {RemoteIP}", 
            id, 
            httpContext.Connection.RemoteIpAddress);

        var command = new DeleteProductCommand(id);
        var result = await messageBus.InvokeAsync<ErrorOr<Success>>(command);
        
        return result.Match(
            _ =>
            {
                logger.LogInformation("DeleteProduct completed - ProductId: {ProductId}", id);
                return Results.NoContent();
            },
            errors => MapErrorsToResult(errors)
        );
    }

    private static async Task<IResult> SyncProductCache(IMessageBus messageBus, ILogger<Program> logger)
    {
        logger.LogInformation("SyncProductCache called - Republishing events for all products");

        // Use search with no filters and large page size to get all products
        var query = new SearchProductsQuery(
            SearchTerm: null,
            Category: null,
            MinPrice: null,
            MaxPrice: null,
            Page: 1,
            PageSize: 10000 // Get all products
        );
        
        var queryResult = await messageBus.InvokeAsync<ErrorOr<SearchProductsResult>>(query);
        
        if (queryResult.IsError)
        {
            logger.LogError("SyncProductCache failed - Unable to retrieve products: {@Errors}", 
                queryResult.Errors.Select(e => new { e.Type, e.Description }));
            return MapErrorsToResult(queryResult.Errors);
        }

        var products = queryResult.Value.Products;
        var publishedCount = 0;

        // Use the domain event system to republish events
        foreach (var product in products)
        {
            var updateCommand = new UpdateProductCommand(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.Category,
                product.ImageUrl
            );
            
            var updateResult = await messageBus.InvokeAsync<ErrorOr<Product>>(updateCommand);
            
            if (updateResult.IsError)
            {
                logger.LogWarning("Failed to republish event for product {ProductId}: {@Errors}", 
                    product.Id, 
                    updateResult.Errors.Select(e => new { e.Type, e.Description }));
            }
            else
            {
                publishedCount++;
            }
        }

        logger.LogInformation("SyncProductCache completed - Republished events for {Count} products", publishedCount);
        return Results.Ok(new { message = $"Republished events for {publishedCount} products", count = publishedCount });
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
