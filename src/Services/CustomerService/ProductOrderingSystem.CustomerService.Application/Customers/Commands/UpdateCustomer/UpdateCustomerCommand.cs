using ErrorOr;
using FluentValidation;
using MediatR;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.CreateCustomer;
using ProductOrderingSystem.CustomerService.Domain.Repositories;

namespace ProductOrderingSystem.CustomerService.Application.Customers.Commands.UpdateCustomer;

public record UpdateCustomerCommand(
    Guid CustomerId,
    string FirstName,
    string LastName,
    string? PhoneNumber) : IRequest<ErrorOr<CustomerResponse>>;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        
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

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, ErrorOr<CustomerResponse>>
{
    private readonly ICustomerRepository _repository;

    public UpdateCustomerCommandHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<CustomerResponse>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.CustomerId, cancellationToken);
        
        if (customer == null)
        {
            return Error.NotFound("Customer.NotFound", $"Customer with ID '{request.CustomerId}' not found");
        }

        customer.UpdateDetails(request.FirstName, request.LastName, request.PhoneNumber);

        var updated = await _repository.UpdateAsync(customer, cancellationToken);

        return CustomerResponse.FromCustomer(updated);
    }
}
