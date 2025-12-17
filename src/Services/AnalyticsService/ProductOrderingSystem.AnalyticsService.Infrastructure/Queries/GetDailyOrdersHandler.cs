using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.AnalyticsService.Application.Queries;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Data;

namespace ProductOrderingSystem.AnalyticsService.Infrastructure.Queries;

public class GetDailyOrdersHandler : IRequestHandler<GetDailyOrders.Query, List<GetDailyOrders.Result>>
{
    private readonly AnalyticsDbContext _context;

    public GetDailyOrdersHandler(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<List<GetDailyOrders.Result>> Handle(GetDailyOrders.Query request, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-request.Days);

        var dailyOrders = await _context.OrderEvents
            .Where(o => o.EventTimestamp >= startDate)
            .GroupBy(o => o.EventTimestamp.Date)
            .Select(g => new GetDailyOrders.Result(
                g.Key,
                g.Count(),
                g.Sum(o => o.TotalAmount)
            ))
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return dailyOrders;
    }
}
