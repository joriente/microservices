using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace ProductOrderingSystem.Web.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    private const string TokenKey = "authToken";

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync(TokenKey);
        
        // Remove quotes if present (SetItemAsStringAsync adds them)
        if (!string.IsNullOrWhiteSpace(token))
        {
            token = token.Trim('"');
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            var claims = ParseClaimsFromJwt(token);
            
            // Check if token is expired
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim != null)
            {
                var expTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value));
                if (expTime <= DateTimeOffset.UtcNow)
                {
                    // Token expired, clear it
                    await _localStorage.RemoveItemAsync(TokenKey);
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            // Set the authorization header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return new AuthenticationState(user);
        }
        catch
        {
            // If token is invalid, clear it
            await _localStorage.RemoveItemAsync(TokenKey);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        // Remove quotes if present
        token = token.Trim('"');
        
        await _localStorage.SetItemAsStringAsync(TokenKey, token);
        
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        // Set the authorization header
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        
        // Clear the authorization header
        _httpClient.DefaultRequestHeaders.Authorization = null;
        
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        return token.Claims;
    }
}
