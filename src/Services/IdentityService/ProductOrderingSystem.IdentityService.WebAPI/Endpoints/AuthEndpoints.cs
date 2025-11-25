using System.Security.Claims;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductOrderingSystem.IdentityService.Application.Commands.Auth;
using ProductOrderingSystem.IdentityService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Identity;

namespace ProductOrderingSystem.IdentityService.WebAPI.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new user (returns 201 with Location header)")
            .Produces(StatusCodes.Status201Created) // 201 Created with Location header, no body
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Login with email/username and password")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get current authenticated user")
            .RequireAuthorization()
            .Produces<UserDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Debug endpoint to check if admin user exists (remove in production)
        group.MapGet("/debug/admin-exists", CheckAdminExists)
            .WithName("CheckAdminExists")
            .WithSummary("Check if admin user exists (debug only)")
            .Produces<object>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.Username,
            request.Password,
            request.FirstName,
            request.LastName);

        var result = await sender.Send(command, cancellationToken);

        return result.Match(
            userDto => 
            {
                // Follow REST principles: 201 Created with Location header, empty body
                var locationUri = $"/api/auth/users/{userDto.Id}";
                httpContext.Response.Headers.Location = locationUri;
                return Results.StatusCode(201); // 201 Created with no body
            },
            errors => MapErrorsToResult(errors));
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        ISender sender,
        ILogger<Program> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Login attempt - EmailOrUsername: {EmailOrUsername}, RemoteIP: {RemoteIP}", 
            request.EmailOrUsername, 
            httpContext.Connection.RemoteIpAddress);

        var command = new LoginCommand(
            request.EmailOrUsername,
            request.Password);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsError)
        {
            logger.LogWarning("Login failed - EmailOrUsername: {EmailOrUsername}, Errors: {Errors}", 
                request.EmailOrUsername,
                string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
        }
        else
        {
            logger.LogInformation("Login successful - EmailOrUsername: {EmailOrUsername}, UserId: {UserId}", 
                request.EmailOrUsername,
                result.Value.User.Id);
        }

        return result.Match(
            loginResponse => Results.Ok(loginResponse),
            errors => MapErrorsToResult(errors));
    }

    private static IResult GetCurrentUser(ClaimsPrincipal claimsPrincipal)
    {
        var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
        var username = claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value;
        var firstName = claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value;
        var lastName = claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value;
        var roles = claimsPrincipal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "User ID not found in token claims");
        }

        var userDto = new UserDto(
            Id: userId,
            Email: email ?? "",
            Username: username ?? "",
            FirstName: firstName ?? "",
            LastName: lastName ?? "",
            Roles: roles,
            IsActive: true); // Active if they have a valid token

        return Results.Ok(userDto);
    }

    private static async Task<IResult> CheckAdminExists(
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var adminUser = await userRepository.GetByUsernameAsync("admin", cancellationToken);
        
        if (adminUser == null)
        {
            return Results.Ok(new { exists = false, message = "Admin user not found" });
        }

        return Results.Ok(new 
        { 
            exists = true, 
            userId = adminUser.Id,
            username = adminUser.Username,
            email = adminUser.Email,
            roles = adminUser.Roles,
            isActive = adminUser.IsActive,
            createdAt = adminUser.CreatedAt
        });
    }

    private static IResult MapErrorsToResult(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Results.Problem();
        }

        var firstError = errors[0];

        return firstError.Type switch
        {
            ErrorType.Validation => Results.ValidationProblem(
                errors.ToDictionary(
                    e => e.Code,
                    e => new[] { e.Description })),
            ErrorType.NotFound => Results.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = firstError.Code,
                Detail = firstError.Description
            }),
            ErrorType.Conflict => Results.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = firstError.Code,
                Detail = firstError.Description
            }),
            ErrorType.Forbidden => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: firstError.Code,
                detail: firstError.Description),
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: firstError.Code,
                detail: firstError.Description)
        };
    }
}
