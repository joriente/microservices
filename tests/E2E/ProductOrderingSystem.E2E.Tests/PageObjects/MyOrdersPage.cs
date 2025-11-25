using Microsoft.Playwright;

namespace ProductOrderingSystem.E2E.Tests.PageObjects;

public class MyOrdersPage
{
    private readonly IPage _page;

    public MyOrdersPage(IPage page)
    {
        _page = page;
    }

    // Locators
    private ILocator OrderItems => _page.Locator(".order-item, .order-card, .mud-card");
    private ILocator EmptyOrdersMessage => _page.Locator(".no-orders, :has-text('No orders found')");
    private ILocator LoadingSpinner => _page.Locator(".loading, .mud-progress-circular");

    // Navigation
    public async Task NavigateAsync()
    {
        await _page.GotoAsync("/myorders");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await WaitForOrdersToLoadAsync();
    }

    // Actions
    public async Task ViewOrderDetailsAsync(string orderId)
    {
        var orderCard = _page.Locator($".order-item:has-text('{orderId}'), .order-card:has-text('{orderId}')");
        await orderCard.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task ViewOrderDetailsByIndexAsync(int index)
    {
        var orderCard = OrderItems.Nth(index);
        await orderCard.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Verification
    public async Task<int> GetOrderCountAsync()
    {
        await WaitForOrdersToLoadAsync();
        
        if (await EmptyOrdersMessage.IsVisibleAsync())
        {
            return 0;
        }
        
        return await OrderItems.CountAsync();
    }

    public async Task<bool> HasOrderAsync(string orderId)
    {
        await WaitForOrdersToLoadAsync();
        var orderCard = _page.Locator($".order-item:has-text('{orderId}'), .order-card:has-text('{orderId}')");
        return await orderCard.IsVisibleAsync();
    }

    public async Task<bool> IsEmptyAsync()
    {
        await WaitForOrdersToLoadAsync();
        return await EmptyOrdersMessage.IsVisibleAsync();
    }

    public async Task<string> GetMostRecentOrderIdAsync()
    {
        await WaitForOrdersToLoadAsync();
        
        if (await OrderItems.CountAsync() == 0)
        {
            return string.Empty;
        }

        var firstOrder = OrderItems.First;
        var orderText = await firstOrder.TextContentAsync() ?? string.Empty;
        
        // Extract order ID from text (format may vary: "Order #123" or "Order: 123" or just the ID)
        var match = System.Text.RegularExpressions.Regex.Match(orderText, @"Order[:\s#]*([A-Za-z0-9-]+)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    public async Task<List<string>> GetAllOrderIdsAsync()
    {
        await WaitForOrdersToLoadAsync();
        
        var orderIds = new List<string>();
        var count = await OrderItems.CountAsync();
        
        for (int i = 0; i < count; i++)
        {
            var orderItem = OrderItems.Nth(i);
            var orderText = await orderItem.TextContentAsync() ?? string.Empty;
            var match = System.Text.RegularExpressions.Regex.Match(orderText, @"Order[:\s#]*([A-Za-z0-9-]+)");
            
            if (match.Success)
            {
                orderIds.Add(match.Groups[1].Value);
            }
        }
        
        return orderIds;
    }

    private async Task WaitForOrdersToLoadAsync()
    {
        // Wait for loading spinner to disappear if present
        if (await LoadingSpinner.IsVisibleAsync())
        {
            await LoadingSpinner.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 10000 });
        }
        
        // Small delay to ensure data is loaded
        await _page.WaitForTimeoutAsync(500);
    }
}
