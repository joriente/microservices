using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.AnalyticsService.Application.Queries;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Data;

namespace ProductOrderingSystem.AnalyticsService.Infrastructure.Queries;

public class GetPopularProductsHandler
{
    private readonly AnalyticsDbContext _context;

    public GetPopularProductsHandler(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<List<GetPopularProducts.Result>> Handle(GetPopularProducts.Query request, CancellationToken cancellationToken)
    {
        var popularProducts = await _context.InventoryEvents
            .Where(e => e.EventType == "Reserved")
            .GroupBy(e => e.ProductId)
            .Select(g => new GetPopularProducts.Result(
                g.Key,
                g.Count(),
                g.Sum(e => Math.Abs(e.QuantityChange))
            ))
            .OrderByDescending(x => x.TotalQuantity)
            .Take(request.Top)
            .ToListAsync(cancellationToken);

        return popularProducts;
    }
}
