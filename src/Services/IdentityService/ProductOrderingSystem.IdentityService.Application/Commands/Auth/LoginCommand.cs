using ErrorOr;
using ProductOrderingSystem.Shared.Contracts.Identity;

namespace ProductOrderingSystem.IdentityService.Application.Commands.Auth;

public record LoginCommand(
    string EmailOrUsername,
    string Password) : IRequest<ErrorOr<LoginResponse>>;
