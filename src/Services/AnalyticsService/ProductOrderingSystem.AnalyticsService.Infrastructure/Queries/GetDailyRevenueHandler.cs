using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.AnalyticsService.Application.Queries;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Data;

namespace ProductOrderingSystem.AnalyticsService.Infrastructure.Queries;

public class GetDailyRevenueHandler : IRequestHandler<GetDailyRevenue.Query, List<GetDailyRevenue.Result>>
{
    private readonly AnalyticsDbContext _context;

    public GetDailyRevenueHandler(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<List<GetDailyRevenue.Result>> Handle(GetDailyRevenue.Query request, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-request.Days);

        var dailyRevenue = await _context.PaymentEvents
            .Where(p => p.EventTimestamp >= startDate && p.Status == "succeeded")
            .GroupBy(p => p.EventTimestamp.Date)
            .Select(g => new GetDailyRevenue.Result(
                g.Key,
                g.Sum(p => p.Amount),
                g.Count()
            ))
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return dailyRevenue;
    }
}
