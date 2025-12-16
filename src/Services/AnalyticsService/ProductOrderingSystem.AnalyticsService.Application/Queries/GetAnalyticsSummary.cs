using MediatR;

namespace ProductOrderingSystem.AnalyticsService.Application.Queries;

public static class GetAnalyticsSummary
{
    public record Query : IRequest<Result>;

    public record Result(
        int TotalOrders,
        int TotalPayments,
        int SuccessfulPayments,
        decimal TotalRevenue,
        int TotalProducts,
        int TotalInventoryReservations
    );
}
