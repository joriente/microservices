using MediatR;

namespace ProductOrderingSystem.AnalyticsService.Application.Queries;

public static class GetPopularProducts
{
    public record Query(int Top = 10) : IRequest<List<Result>>;

    public record Result(
        Guid ProductId,
        int ReservationCount,
        int TotalQuantity
    );
}
