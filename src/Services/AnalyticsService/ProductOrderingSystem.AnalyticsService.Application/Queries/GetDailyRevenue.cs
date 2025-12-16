using MediatR;

namespace ProductOrderingSystem.AnalyticsService.Application.Queries;

public static class GetDailyRevenue
{
    public record Query(int Days = 7) : IRequest<List<Result>>;

    public record Result(
        DateTime Date,
        decimal Revenue,
        int PaymentCount
    );
}
