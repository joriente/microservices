namespace ProductOrderingSystem.AnalyticsService.Application.Queries;

public static class GetDailyOrders
{
    public record Query(int Days = 7);

    public record Result(
        DateTime Date,
        int OrderCount,
        decimal TotalAmount
    );
}
