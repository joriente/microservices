using Wolverine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.AddAddress;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.CreateCustomer;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.UpdateCustomer;
using ProductOrderingSystem.CustomerService.Application.Customers.Queries.GetCustomerById;
using ProductOrderingSystem.CustomerService.Application.Customers.Queries.GetCustomers;
using ProductOrderingSystem.Shared.Contracts.Customers;
using DomainAddressType = ProductOrderingSystem.CustomerService.Domain.ValueObjects.AddressType;

namespace ProductOrderingSystem.CustomerService.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(IMessageBus messageBus, ILogger<CustomersController> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var query = new GetCustomersQuery(skip, pageSize);
        var result = await _messageBus.InvokeAsync<ErrorOr.ErrorOr<GetCustomersResponse>>(query);

        return result.Match(
            success =>
            {
                // Calculate pagination metadata
                var totalPages = (int)Math.Ceiling(success.Total / (double)pageSize);
                var paginationMetadata = new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = success.Total,
                    TotalPages = totalPages,
                    HasPrevious = page > 1,
                    HasNext = page < totalPages
                };

                // Add pagination metadata to response header as JSON
                Response.Headers["X-Pagination"] = System.Text.Json.JsonSerializer.Serialize(paginationMetadata);

                // Return only the customers array in the body
                return Ok(success.Customers);
            },
            errors => Problem(errors));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetCustomerByIdQuery(id);
        var result = await _messageBus.InvokeAsync<ErrorOr.ErrorOr<CustomerResponse>>(query);

        return result.Match(
            success => Ok(success),
            errors => Problem(errors));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var command = new CreateCustomerCommand(
            request.Email,
            request.FirstName,
            request.LastName,
            request.PhoneNumber);

        var result = await _messageBus.InvokeAsync<ErrorOr.ErrorOr<CustomerResponse>>(command);

        return result.Match(
            success => CreatedAtAction(nameof(GetById), new { id = success.Id }, success), // Return 201 + Location header + customer in body
            errors => Problem(errors));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var command = new UpdateCustomerCommand(
            id,
            request.FirstName,
            request.LastName,
            request.PhoneNumber);

        var result = await _messageBus.InvokeAsync<ErrorOr.ErrorOr<CustomerResponse>>(command);

        return result.Match(
            success => Ok(success),
            errors => Problem(errors));
    }

    [HttpPost("{id:guid}/addresses")]
    public async Task<IActionResult> AddAddress(Guid id, [FromBody] AddAddressRequest request)
    {
        var command = new AddAddressCommand(
            id,
            request.Street,
            request.City,
            request.State,
            request.PostalCode,
            request.Country,
            request.IsDefault,
            (DomainAddressType)request.Type); // Map from Shared.Contracts to Domain

        var result = await _messageBus.InvokeAsync<ErrorOr.ErrorOr<CustomerResponse>>(command);

        return result.Match(
            success => Ok(success),
            errors => Problem(errors));
    }

    private IActionResult Problem(List<ErrorOr.Error> errors)
    {
        var firstError = errors.First();

        var statusCode = firstError.Type switch
        {
            ErrorOr.ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorOr.ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorOr.ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, title: firstError.Description);
    }
}
