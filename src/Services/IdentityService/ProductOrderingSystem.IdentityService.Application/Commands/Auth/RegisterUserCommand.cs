using ErrorOr;
using ProductOrderingSystem.Shared.Contracts.Identity;

namespace ProductOrderingSystem.IdentityService.Application.Commands.Auth;

public record RegisterUserCommand(
    string Email,
    string Username,
    string Password,
    string FirstName,
    string LastName);
