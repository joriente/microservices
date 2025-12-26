using Wolverine;
using Microsoft.AspNetCore.Http.HttpResults;
using ProductOrderingSystem.AnalyticsService.Application.Queries;

namespace ProductOrderingSystem.AnalyticsService.WebAPI.Endpoints;

public static class AnalyticsEndpoints
{
    public static RouteGroupBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/analytics")
            .WithTags("Analytics");

        group.MapGet("/summary", GetSummary)
            .WithName("GetAnalyticsSummary")
            .WithSummary("Get overall analytics summary");

        group.MapGet("/orders/daily", GetDailyOrders)
            .WithName("GetDailyOrders")
            .WithSummary("Get daily order statistics");

        group.MapGet("/revenue/daily", GetDailyRevenue)
            .WithName("GetDailyRevenue")
            .WithSummary("Get daily revenue statistics");

        group.MapGet("/products/popular", GetPopularProducts)
            .WithName("GetPopularProducts")
            .WithSummary("Get most popular products");

        group.MapGet("/health", GetHealth)
            .WithName("GetAnalyticsHealth")
            .WithSummary("Health check for analytics service");

        return group;
    }

    private static async Task<Ok<GetAnalyticsSummary.Result>> GetSummary(
        IMessageBus messageBus)
    {
        var result = await messageBus.InvokeAsync<GetAnalyticsSummary.Result>(new GetAnalyticsSummary.Query());
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<List<GetDailyOrders.Result>>> GetDailyOrders(
        IMessageBus messageBus,
        int days = 7)
    {
        var result = await messageBus.InvokeAsync<List<GetDailyOrders.Result>>(new GetDailyOrders.Query(days));
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<List<GetDailyRevenue.Result>>> GetDailyRevenue(
        IMessageBus messageBus,
        int days = 7)
    {
        var result = await messageBus.InvokeAsync<List<GetDailyRevenue.Result>>(new GetDailyRevenue.Query(days));
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<List<GetPopularProducts.Result>>> GetPopularProducts(
        IMessageBus messageBus,
        int top = 10)
    {
        var result = await messageBus.InvokeAsync<List<GetPopularProducts.Result>>(new GetPopularProducts.Query(top));
        return TypedResults.Ok(result);
    }

    private static Ok<object> GetHealth()
    {
        return TypedResults.Ok<object>(new { Status = "Healthy", Service = "Analytics Service" });
    }
}
