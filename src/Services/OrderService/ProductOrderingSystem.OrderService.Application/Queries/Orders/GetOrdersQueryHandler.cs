using ErrorOr;
using MediatR;
using ProductOrderingSystem.OrderService.Domain.Entities;
using ProductOrderingSystem.OrderService.Domain.Repositories;

namespace ProductOrderingSystem.OrderService.Application.Queries.Orders;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, ErrorOr<GetOrdersResult>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ErrorOr<GetOrdersResult>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        // Validate pagination parameters
        if (request.Page < 1)
            return Error.Validation("Query.Page", "Page must be greater than 0");

        if (request.PageSize < 1 || request.PageSize > 100)
            return Error.Validation("Query.PageSize", "Page size must be between 1 and 100");

        // Validate date range if provided
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate > request.EndDate)
            return Error.Validation("Query.DateRange", "Start date cannot be after end date");

        // Get filtered and paginated orders
        var (orders, totalCount) = await _orderRepository.GetOrdersAsync(
            request.CustomerId,
            request.Status,
            request.StartDate,
            request.EndDate,
            request.Page,
            request.PageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var result = new GetOrdersResult(
            Orders: orders.ToList(),
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages
        );

        return result;
    }
}
