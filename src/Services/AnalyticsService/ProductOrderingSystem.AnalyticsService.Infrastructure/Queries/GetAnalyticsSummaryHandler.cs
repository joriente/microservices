using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.AnalyticsService.Application.Queries;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Data;

namespace ProductOrderingSystem.AnalyticsService.Infrastructure.Queries;

public class GetAnalyticsSummaryHandler : IRequestHandler<GetAnalyticsSummary.Query, GetAnalyticsSummary.Result>
{
    private readonly AnalyticsDbContext _context;

    public GetAnalyticsSummaryHandler(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<GetAnalyticsSummary.Result> Handle(GetAnalyticsSummary.Query request, CancellationToken cancellationToken)
    {
        var totalOrders = await _context.OrderEvents.CountAsync(cancellationToken);
        var totalPayments = await _context.PaymentEvents.CountAsync(cancellationToken);
        var successfulPayments = await _context.PaymentEvents
            .CountAsync(p => p.Status == "succeeded", cancellationToken);
        var totalRevenue = await _context.PaymentEvents
            .Where(p => p.Status == "succeeded")
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;
        var totalProducts = await _context.ProductEvents.CountAsync(cancellationToken);
        var totalInventoryReservations = await _context.InventoryEvents.CountAsync(cancellationToken);

        return new GetAnalyticsSummary.Result(
            totalOrders,
            totalPayments,
            successfulPayments,
            totalRevenue,
            totalProducts,
            totalInventoryReservations
        );
    }
}
