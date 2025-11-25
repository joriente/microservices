namespace ProductOrderingSystem.Shared.Contracts.Identity;

public record RegisterRequest(
    string Email,
    string Username,
    string Password,
    string FirstName,
    string LastName);

public record LoginRequest(
    string EmailOrUsername,
    string Password);

public record LoginResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public record RefreshTokenRequest(
    string RefreshToken);

public record UserDto(
    string Id,
    string Email,
    string Username,
    string FirstName,
    string LastName,
    List<string> Roles,
    bool IsActive);
