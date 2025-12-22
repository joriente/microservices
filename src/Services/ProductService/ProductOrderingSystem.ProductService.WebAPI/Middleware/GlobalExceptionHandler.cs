using Microsoft.AspNetCore.Diagnostics;
using ProductOrderingSystem.ProductService.Domain.Exceptions;
using System.Net;

namespace ProductOrderingSystem.ProductService.WebAPI.Middleware;

/// <summary>
/// Global exception handler for Wolverine - converts exceptions to appropriate HTTP responses
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, details) = exception switch
        {
            ProductNotFoundException notFound => (
                HttpStatusCode.NotFound,
                "Product Not Found",
                notFound.Message
            ),
            ProductValidationException validation => (
                HttpStatusCode.BadRequest,
                "Validation Error",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    message = validation.Message,
                    errors = validation.Errors
                })
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                exception.Message
            )
        };

        _logger.LogError(
            exception,
            "Exception occurred: {Message}. Status: {StatusCode}",
            exception.Message,
            statusCode);

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        var problemDetails = new
        {
            status = (int)statusCode,
            title = title,
            detail = details,
            traceId = httpContext.TraceIdentifier
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
