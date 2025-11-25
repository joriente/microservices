using Microsoft.Playwright;

namespace ProductOrderingSystem.E2E.Tests.PageObjects;

public class CheckoutPage
{
    private readonly IPage _page;

    public CheckoutPage(IPage page)
    {
        _page = page;
    }

    // Locators - Shipping Address
    private ILocator StreetInput => _page.GetByLabel("Street Address");
    private ILocator CityInput => _page.GetByLabel("City");
    private ILocator StateInput => _page.GetByLabel("State");
    private ILocator PostalCodeInput => _page.GetByLabel("Postal Code", new() { Exact = false });
    private ILocator CountryInput => _page.GetByLabel("Country");

    // Locators - Payment (Stripe)
    private IFrameLocator CardNumberFrame => _page.FrameLocator("iframe[name*='card-number'], iframe[title*='card number']");
    private IFrameLocator CardExpiryFrame => _page.FrameLocator("iframe[name*='card-expiry'], iframe[title*='expiration']");
    private IFrameLocator CardCvcFrame => _page.FrameLocator("iframe[name*='card-cvc'], iframe[title*='security']");
    
    private ILocator OrderSummary => _page.Locator(".order-summary, .checkout-summary");
    private ILocator PlaceOrderButton => _page.GetByRole(AriaRole.Button, new() { Name = "Place Order" });
    private ILocator ProcessingIndicator => _page.Locator(".processing, .loading");
    private ILocator SuccessMessage => _page.Locator(".success-message, .order-confirmation");
    private ILocator ErrorMessage => _page.Locator(".error-message, .mud-alert-error");

    // Navigation
    public async Task NavigateAsync()
    {
        await _page.GotoAsync("/checkout");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // Actions - Fill Shipping Address
    public async Task FillShippingAddressAsync(
        string street, 
        string city, 
        string state, 
        string postalCode, 
        string country = "USA")
    {
        await StreetInput.FillAsync(street);
        await CityInput.FillAsync(city);
        await StateInput.FillAsync(state);
        await PostalCodeInput.FillAsync(postalCode);
        await CountryInput.FillAsync(country);
    }

    // Actions - Fill Payment Information (Stripe)
    public async Task FillPaymentInformationAsync(
        string cardNumber, 
        string expiry = "12/30", 
        string cvc = "123")
    {
        // Wait for Stripe iframes to load
        await _page.WaitForTimeoutAsync(2000);

        try
        {
            // Fill card number
            var cardNumberInput = CardNumberFrame.Locator("input[name='cardnumber'], input[placeholder*='Card number']");
            await cardNumberInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await cardNumberInput.FillAsync(cardNumber);

            // Fill expiry
            var expiryInput = CardExpiryFrame.Locator("input[name='exp-date'], input[placeholder*='MM']");
            await expiryInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await expiryInput.FillAsync(expiry);

            // Fill CVC
            var cvcInput = CardCvcFrame.Locator("input[name='cvc'], input[placeholder*='CVC']");
            await cvcInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await cvcInput.FillAsync(cvc);
        }
        catch (TimeoutException)
        {
            // If Stripe Elements aren't in iframes, try direct input (fallback for test environment)
            var cardInput = _page.Locator("input[placeholder*='Card number'], input[name='cardNumber']");
            if (await cardInput.IsVisibleAsync())
            {
                await cardInput.FillAsync(cardNumber);
                
                var expiryInput = _page.Locator("input[placeholder*='Expiry'], input[name='expiry']");
                await expiryInput.FillAsync(expiry);
                
                var cvcInput = _page.Locator("input[placeholder*='CVC'], input[name='cvc']");
                await cvcInput.FillAsync(cvc);
            }
        }
    }

    public async Task PlaceOrderAsync()
    {
        await PlaceOrderButton.ClickAsync();
        
        // Wait for processing
        if (await ProcessingIndicator.IsVisibleAsync())
        {
            await ProcessingIndicator.WaitForAsync(new() 
            { 
                State = WaitForSelectorState.Hidden, 
                Timeout = 60000 // Long timeout for payment processing
            });
        }
    }

    public async Task CompleteCheckoutAsync(
        string street,
        string city,
        string state,
        string postalCode,
        string cardNumber,
        string country = "USA",
        string expiry = "12/30",
        string cvc = "123")
    {
        await FillShippingAddressAsync(street, city, state, postalCode, country);
        await FillPaymentInformationAsync(cardNumber, expiry, cvc);
        await PlaceOrderAsync();
    }

    // Verification
    public async Task<bool> IsSuccessMessageVisibleAsync()
    {
        try
        {
            await SuccessMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60000 });
            return await SuccessMessage.IsVisibleAsync();
        }
        catch (TimeoutException)
        {
            return false;
        }
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

    public async Task<string> GetOrderSummaryAsync()
    {
        return await OrderSummary.TextContentAsync() ?? string.Empty;
    }

    public async Task<string?> GetOrderIdFromSuccessMessageAsync()
    {
        var message = await GetSuccessMessageAsync();
        // Extract order ID from message like "Order #ABC123 placed successfully"
        var match = System.Text.RegularExpressions.Regex.Match(message, @"Order #?([A-Za-z0-9-]+)");
        return match.Success ? match.Groups[1].Value : null;
    }
}
