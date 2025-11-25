namespace ProductOrderingSystem.Shared.Contracts.Users
{
    public record UserDto(
        string Id,
        string Email,
        string FirstName,
        string LastName,
        string? PhoneNumber,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public record CreateUserRequest(
        string Email,
        string FirstName,
        string LastName,
        string? PhoneNumber,
        string Password
    );

    public record UpdateUserRequest(
        string Id,
        string Email,
        string FirstName,
        string LastName,
        string? PhoneNumber
    );

    public record LoginRequest(
        string Email,
        string Password
    );

    public record LoginResponse(
        string Token,
        UserDto User
    );
}