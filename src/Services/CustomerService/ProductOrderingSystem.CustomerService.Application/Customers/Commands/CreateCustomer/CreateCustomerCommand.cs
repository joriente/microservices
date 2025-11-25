using ErrorOr;
using FluentValidation;
using MediatR;
using ProductOrderingSystem.CustomerService.Domain.Entities;
using ProductOrderingSystem.CustomerService.Domain.Repositories;

namespace ProductOrderingSystem.CustomerService.Application.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber) : IRequest<ErrorOr<CustomerResponse>>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, ErrorOr<CustomerResponse>>
{
    private readonly ICustomerRepository _repository;

    public CreateCustomerCommandHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<CustomerResponse>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        if (await _repository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return Error.Conflict("Customer.EmailExists", $"Customer with email '{request.Email}' already exists");
        }

        // Create customer
        var customer = Customer.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            request.PhoneNumber);

        // Save to database
        var created = await _repository.AddAsync(customer, cancellationToken);

        return CustomerResponse.FromCustomer(created);
    }
}

public record CustomerResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    List<AddressResponse> Addresses,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsActive)
{
    public static CustomerResponse FromCustomer(Customer customer)
    {
        return new CustomerResponse(
            customer.Id,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.PhoneNumber,
            customer.Addresses.Select(a => new AddressResponse(
                a.Id,
                a.Street,
                a.City,
                a.State,
                a.PostalCode,
                a.Country,
                a.IsDefault,
                a.Type.ToString()
            )).ToList(),
            customer.CreatedAt,
            customer.UpdatedAt,
            customer.IsActive
        );
    }
}

public record AddressResponse(
    Guid Id,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault,
    string Type);
