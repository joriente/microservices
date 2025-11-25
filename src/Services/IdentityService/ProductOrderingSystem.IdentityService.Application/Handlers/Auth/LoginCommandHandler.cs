using ErrorOr;
using Microsoft.Extensions.Configuration;
using ProductOrderingSystem.IdentityService.Application.Commands.Auth;
using ProductOrderingSystem.IdentityService.Domain.Repositories;
using ProductOrderingSystem.IdentityService.Domain.Services;
using ProductOrderingSystem.Shared.Contracts.Identity;

namespace ProductOrderingSystem.IdentityService.Application.Handlers.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ErrorOr<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<ErrorOr<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Try to find user by email or username
        var user = await _userRepository.GetByEmailAsync(request.EmailOrUsername, cancellationToken);
        
        if (user is null)
        {
            user = await _userRepository.GetByUsernameAsync(request.EmailOrUsername, cancellationToken);
        }

        if (user is null)
        {
            return Error.NotFound("User", "Invalid credentials");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Error.Validation("Password", "Invalid credentials");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Error.Forbidden("User.IsActive", "User account is deactivated");
        }

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var expirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.Username,
            user.FirstName,
            user.LastName,
            user.Roles,
            user.IsActive);

        return new LoginResponse(
            accessToken,
            refreshToken,
            expiresAt,
            userDto);
    }
}
