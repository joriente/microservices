using Microsoft.Playwright;

namespace ProductOrderingSystem.E2E.Tests.PageObjects;

public class HomePage
{
    private readonly IPage _page;

    public HomePage(IPage page)
    {
        _page = page;
    }

    // Locators - Navigation (MudNavLink in drawer)
    private ILocator HomeLink => _page.GetByRole(AriaRole.Link, new() { Name = "Home" });
    private ILocator ProductsLink => _page.GetByRole(AriaRole.Link, new() { Name = "Products" });
    private ILocator CartLink => _page.GetByRole(AriaRole.Link, new() { Name = "Cart" });
    private ILocator MyOrdersLink => _page.GetByRole(AriaRole.Link, new() { Name = "My Orders" });
    
    // Locators - AppBar buttons (for non-authenticated users)
    private ILocator LoginButton => _page.GetByRole(AriaRole.Link, new() { Name = "Login" });
    private ILocator RegisterButton => _page.GetByRole(AriaRole.Link, new() { Name = "Register" });
    
    // Locators - User menu (for authenticated users)
    private ILocator UserMenuButton => _page.Locator("[aria-label='Account Circle']");
    private ILocator LogoutMenuItem => _page.GetByRole(AriaRole.Menuitem, new() { Name = "Logout" });

    // Navigation methods
    public async Task NavigateAsync()
    {
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GoToProductsAsync()
    {
        await ProductsLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GoToLoginAsync()
    {
        await LoginButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GoToRegisterAsync()
    {
        await RegisterButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GoToCartAsync()
    {
        await CartLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GoToMyOrdersAsync()
    {
        await MyOrdersLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task LogoutAsync()
    {
        await UserMenuButton.ClickAsync();
        await LogoutMenuItem.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Verification methods
    public async Task<bool> IsLoggedInAsync()
    {
        return await UserMenuButton.IsVisibleAsync();
    }

    public async Task<bool> IsLoggedOutAsync()
    {
        return await LoginButton.IsVisibleAsync();
    }
}
