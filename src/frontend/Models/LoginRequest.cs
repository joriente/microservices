namespace ProductOrderingSystem.Web.Models;

public record LoginRequest(string EmailOrUsername, string Password);

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName);

public record AuthResponse(
    string Token,
    User User);

public record User(
    Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName);
