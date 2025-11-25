using ProductOrderingSystem.Web.Models;

namespace ProductOrderingSystem.Web.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<User?> GetCurrentUserAsync();
    Task LogoutAsync();
}
