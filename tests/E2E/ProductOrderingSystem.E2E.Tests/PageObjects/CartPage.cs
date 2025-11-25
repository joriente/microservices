using Microsoft.Playwright;

namespace ProductOrderingSystem.E2E.Tests.PageObjects;

public class CartPage
{
    private readonly IPage _page;

    public CartPage(IPage page)
    {
        _page = page;
    }

    // Locators
    private ILocator CartItems => _page.Locator(".cart-item, .mud-list-item");
    private ILocator EmptyCartMessage => _page.Locator(".empty-cart, :has-text('Your cart is empty')");
    private ILocator CheckoutButton => _page.GetByRole(AriaRole.Button, new() { Name = "Checkout", Exact = true });
    private ILocator ContinueShoppingButton => _page.GetByRole(AriaRole.Button, new() { Name = "Continue Shopping" });
    private ILocator TotalAmount => _page.Locator(".total-amount, .cart-total");
    private ILocator RemoveButtons => _page.GetByRole(AriaRole.Button, new() { Name = "Remove" });

    // Navigation
    public async Task NavigateAsync()
    {
        await _page.GotoAsync("/cart");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Actions
    public async Task GoToCheckoutAsync()
    {
        await CheckoutButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task ContinueShoppingAsync()
    {
        await ContinueShoppingButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task RemoveItemByIndexAsync(int index)
    {
        await RemoveButtons.Nth(index).ClickAsync();
        await _page.WaitForTimeoutAsync(500); // Wait for cart to update
    }

    public async Task RemoveItemByNameAsync(string productName)
    {
        var cartItem = _page.Locator($".cart-item:has-text('{productName}'), .mud-list-item:has-text('{productName}')");
        var removeButton = cartItem.GetByRole(AriaRole.Button, new() { Name = "Remove" });
        await removeButton.ClickAsync();
        await _page.WaitForTimeoutAsync(500); // Wait for cart to update
    }

    public async Task UpdateQuantityAsync(string productName, int quantity)
    {
        var cartItem = _page.Locator($".cart-item:has-text('{productName}'), .mud-list-item:has-text('{productName}')");
        var quantityInput = cartItem.Locator("input[type='number'], .quantity-input");
        await quantityInput.FillAsync(quantity.ToString());
        await quantityInput.PressAsync("Enter");
        await _page.WaitForTimeoutAsync(500); // Wait for cart to update
    }

    // Verification
    public async Task<bool> IsCartEmptyAsync()
    {
        return await EmptyCartMessage.IsVisibleAsync();
    }

    public async Task<int> GetItemCountAsync()
    {
        if (await IsCartEmptyAsync())
        {
            return 0;
        }
        return await CartItems.CountAsync();
    }

    public async Task<bool> ContainsProductAsync(string productName)
    {
        var cartItem = _page.Locator($".cart-item:has-text('{productName}'), .mud-list-item:has-text('{productName}')");
        return await cartItem.IsVisibleAsync();
    }

    public async Task<string> GetTotalAmountAsync()
    {
        return await TotalAmount.TextContentAsync() ?? string.Empty;
    }

    public async Task<bool> IsCheckoutButtonEnabledAsync()
    {
        return await CheckoutButton.IsEnabledAsync();
    }
}
