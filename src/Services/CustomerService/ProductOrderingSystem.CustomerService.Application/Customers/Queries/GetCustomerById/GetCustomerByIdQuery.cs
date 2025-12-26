using ErrorOr;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.CreateCustomer;
using ProductOrderingSystem.CustomerService.Domain.Repositories;

namespace ProductOrderingSystem.CustomerService.Application.Customers.Queries.GetCustomerById;

public record GetCustomerByIdQuery(Guid CustomerId);

public class GetCustomerByIdQueryHandler
{
    private readonly ICustomerRepository _repository;

    public GetCustomerByIdQueryHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<CustomerResponse>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.CustomerId, cancellationToken);
        
        if (customer == null)
        {
            return Error.NotFound("Customer.NotFound", $"Customer with ID '{request.CustomerId}' not found");
        }

        return CustomerResponse.FromCustomer(customer);
    }
}
