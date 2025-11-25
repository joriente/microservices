using Microsoft.Playwright;

namespace ProductOrderingSystem.E2E.Tests.PageObjects;

public class ProductsPage
{
    private readonly IPage _page;

    public ProductsPage(IPage page)
    {
        _page = page;
    }

    // Locators
    private ILocator ProductCards => _page.Locator(".mud-card, .product-card");
    private ILocator SearchInput => _page.GetByPlaceholder("Search products");
    private ILocator CategoryFilter => _page.Locator("select[name='category'], .category-filter");
    private ILocator LoadingSpinner => _page.Locator(".mud-progress-circular, .loading");

    // Navigation
    public async Task NavigateAsync()
    {
        await _page.GotoAsync("/products");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await WaitForProductsToLoadAsync();
    }

    // Actions
    public async Task SearchProductsAsync(string searchTerm)
    {
        await SearchInput.FillAsync(searchTerm);
        await _page.Keyboard.PressAsync("Enter");
        await WaitForProductsToLoadAsync();
    }

    public async Task FilterByCategoryAsync(string category)
    {
        await CategoryFilter.SelectOptionAsync(category);
        await WaitForProductsToLoadAsync();
    }

    public async Task AddToCartByProductNameAsync(string productName)
    {
        var productCard = _page.Locator($".mud-card:has-text('{productName}'), .product-card:has-text('{productName}')").First;
        var addToCartButton = productCard.GetByRole(AriaRole.Button, new() { Name = "Add to Cart" });
        await addToCartButton.ClickAsync();
        
        // Wait for the cart to update
        await _page.WaitForTimeoutAsync(500);
    }

    public async Task AddToCartByIndexAsync(int index)
    {
        var productCard = ProductCards.Nth(index);
        var addToCartButton = productCard.GetByRole(AriaRole.Button, new() { Name = "Add to Cart" });
        await addToCartButton.ClickAsync();
        
        // Wait for the cart to update
        await _page.WaitForTimeoutAsync(500);
    }

    public async Task ViewProductDetailsAsync(string productName)
    {
        var productCard = _page.Locator($".mud-card:has-text('{productName}'), .product-card:has-text('{productName}')").First;
        await productCard.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Verification
    public async Task<int> GetProductCountAsync()
    {
        await WaitForProductsToLoadAsync();
        return await ProductCards.CountAsync();
    }

    public async Task<bool> IsProductVisibleAsync(string productName)
    {
        var product = _page.Locator($".mud-card:has-text('{productName}'), .product-card:has-text('{productName}')");
        return await product.IsVisibleAsync();
    }

    public async Task<string> GetProductPriceAsync(string productName)
    {
        var productCard = _page.Locator($".mud-card:has-text('{productName}'), .product-card:has-text('{productName}')").First;
        var priceElement = productCard.Locator(".price, .product-price");
        return await priceElement.TextContentAsync() ?? string.Empty;
    }

    private async Task WaitForProductsToLoadAsync()
    {
        // Wait a moment for initial load
        await _page.WaitForTimeoutAsync(1000);
        
        // Check if loading spinner is visible, if so wait for it to disappear
        try
        {
            if (await LoadingSpinner.IsVisibleAsync())
            {
                await LoadingSpinner.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });
            }
        }
        catch
        {
            // If spinner check fails, continue anyway
        }
        
        // Wait for at least one product card to be visible OR the "no products" message
        try
        {
            await _page.WaitForSelectorAsync(".mud-card, .mud-paper:has-text('No products')", new() { Timeout = 10000 });
        }
        catch
        {
            // If products don't load, that's okay - the test will handle it
        }
    }
}
