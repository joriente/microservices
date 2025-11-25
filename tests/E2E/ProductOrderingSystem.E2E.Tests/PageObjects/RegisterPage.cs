using Microsoft.Playwright;

namespace ProductOrderingSystem.E2E.Tests.PageObjects;

public class RegisterPage
{
    private readonly IPage _page;

    public RegisterPage(IPage page)
    {
        _page = page;
    }

    // Locators
    private ILocator EmailInput => _page.GetByLabel("Email");
    private ILocator UsernameInput => _page.GetByLabel("Username");
    private ILocator PasswordInput => _page.Locator("input[type='password']").First;
    private ILocator ConfirmPasswordInput => _page.Locator("input[type='password']").Last;
    private ILocator FirstNameInput => _page.GetByLabel("First Name");
    private ILocator LastNameInput => _page.GetByLabel("Last Name");
    private ILocator RegisterButton => _page.GetByRole(AriaRole.Button, new() { Name = "Create Account" });
    private ILocator SuccessMessage => _page.Locator(".mud-alert-success, .success-message");
    private ILocator ErrorMessage => _page.Locator(".mud-alert-error, .error-message");
    private ILocator LoginLink => _page.GetByRole(AriaRole.Link, new() { Name = "Login", Exact = true });

    // Navigation
    public async Task NavigateAsync()
    {
        await _page.GotoAsync("/register");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Actions
    public async Task RegisterUserAsync(string email, string username, string password, string firstName, string lastName)
    {
        await EmailInput.FillAsync(email);
        await UsernameInput.FillAsync(username);
        await PasswordInput.FillAsync(password);
        await ConfirmPasswordInput.FillAsync(password);
        await FirstNameInput.FillAsync(firstName);
        await LastNameInput.FillAsync(lastName);
        await RegisterButton.ClickAsync();
        
        // Wait for navigation or response
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GoToLoginAsync()
    {
        await LoginLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Verification
    public async Task<bool> IsSuccessMessageVisibleAsync()
    {
        return await SuccessMessage.IsVisibleAsync();
    }

    public async Task<bool> IsErrorMessageVisibleAsync()
    {
        return await ErrorMessage.IsVisibleAsync();
    }

    public async Task<string> GetSuccessMessageAsync()
    {
        return await SuccessMessage.TextContentAsync() ?? string.Empty;
    }

    public async Task<string> GetErrorMessageAsync()
    {
        return await ErrorMessage.TextContentAsync() ?? string.Empty;
    }
}
