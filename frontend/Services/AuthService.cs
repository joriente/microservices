using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using ProductOrderingSystem.Web.Models;

namespace ProductOrderingSystem.Web.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "authToken";

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null)
                {
                    // Store token (will be trimmed by CustomAuthStateProvider)
                    await _localStorage.SetItemAsStringAsync(TokenKey, authResponse.Token);
                    
                    // Set authorization header
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", authResponse.Token.Trim('"'));
                }
                return authResponse;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            
            if (response.IsSuccessStatusCode)
            {
                // After registration, login with the same credentials
                return await LoginAsync(new LoginRequest(request.Username, request.Password));
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync(TokenKey);
            if (string.IsNullOrEmpty(token))
                return null;

            // Remove quotes if present
            token = token.Trim('"');

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("/api/auth/me");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<User>();
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
}
