using ProductOrderingSystem.IdentityService.Domain.Entities;

namespace ProductOrderingSystem.IdentityService.Domain.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    bool ValidateToken(string token, out string? userId);
}
