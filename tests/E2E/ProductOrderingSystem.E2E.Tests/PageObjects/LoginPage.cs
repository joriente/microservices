using Microsoft.Playwright;

namespace ProductOrderingSystem.E2E.Tests.PageObjects;

public class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page)
    {
        _page = page;
    }

    // Locators
    private ILocator UsernameInput => _page.GetByLabel("Username or Email");
    private ILocator PasswordInput => _page.GetByLabel("Password");
    private ILocator LoginButton => _page.GetByRole(AriaRole.Button, new() { Name = "Sign In" });
    private ILocator ErrorMessage => _page.Locator(".mud-alert-error, .error-message");
    private ILocator RegisterLink => _page.GetByRole(AriaRole.Link, new() { Name = "Register", Exact = true });

    // Navigation
    public async Task NavigateAsync()
    {
        await _page.GotoAsync("/login");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Actions
    public async Task LoginAsync(string username, string password)
    {
        await UsernameInput.FillAsync(username);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
        
        // Wait for navigation after login
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GoToRegisterAsync()
    {
        await RegisterLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Verification
    public async Task<bool> IsErrorMessageVisibleAsync()
    {
        return await ErrorMessage.IsVisibleAsync();
    }

    public async Task<string> GetErrorMessageAsync()
    {
        return await ErrorMessage.TextContentAsync() ?? string.Empty;
    }
}
