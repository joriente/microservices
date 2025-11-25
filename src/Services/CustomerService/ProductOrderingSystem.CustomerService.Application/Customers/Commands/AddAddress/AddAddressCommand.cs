using ErrorOr;
using FluentValidation;
using MediatR;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.CreateCustomer;
using ProductOrderingSystem.CustomerService.Domain.Repositories;
using ProductOrderingSystem.CustomerService.Domain.ValueObjects;

namespace ProductOrderingSystem.CustomerService.Application.Customers.Commands.AddAddress;

public record AddAddressCommand(
    Guid CustomerId,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault,
    AddressType Type) : IRequest<ErrorOr<CustomerResponse>>;

public class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
    public AddAddressCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}

public class AddAddressCommandHandler : IRequestHandler<AddAddressCommand, ErrorOr<CustomerResponse>>
{
    private readonly ICustomerRepository _repository;

    public AddAddressCommandHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<CustomerResponse>> Handle(AddAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.CustomerId, cancellationToken);
        
        if (customer == null)
        {
            return Error.NotFound("Customer.NotFound", $"Customer with ID '{request.CustomerId}' not found");
        }

        var address = new Address
        {
            Street = request.Street,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country,
            IsDefault = request.IsDefault,
            Type = request.Type
        };

        customer.AddAddress(address);

        var updated = await _repository.UpdateAsync(customer, cancellationToken);

        return CustomerResponse.FromCustomer(updated);
    }
}
