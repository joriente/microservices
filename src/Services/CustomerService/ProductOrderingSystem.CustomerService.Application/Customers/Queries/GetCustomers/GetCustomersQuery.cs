using ErrorOr;
using MediatR;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.CreateCustomer;
using ProductOrderingSystem.CustomerService.Domain.Repositories;

namespace ProductOrderingSystem.CustomerService.Application.Customers.Queries.GetCustomers;

public record GetCustomersQuery(int Skip = 0, int Limit = 100) : IRequest<ErrorOr<GetCustomersResponse>>;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, ErrorOr<GetCustomersResponse>>
{
    private readonly ICustomerRepository _repository;

    public GetCustomersQueryHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<GetCustomersResponse>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await _repository.GetAllAsync(request.Skip, request.Limit, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);

        return new GetCustomersResponse(
            customers.Select(CustomerResponse.FromCustomer).ToList(),
            total,
            request.Skip,
            request.Limit
        );
    }
}

public record GetCustomersResponse(
    List<CustomerResponse> Customers,
    long Total,
    int Skip,
    int Limit);
