using ProductOrderingSystem.E2E.Tests.Configuration;
using ProductOrderingSystem.E2E.Tests.PageObjects;

namespace ProductOrderingSystem.E2E.Tests.UserFlows;

[TestFixture]
[Description("End-to-end tests for complete shopping flow: Register → Browse → Cart → Checkout → View Orders")]
public class CompleteShoppingFlowTests : PlaywrightTest
{
    private HomePage _homePage = null!;
    private RegisterPage _registerPage = null!;
    private LoginPage _loginPage = null!;
    private ProductsPage _productsPage = null!;
    private CartPage _cartPage = null!;
    private CheckoutPage _checkoutPage = null!;
    private MyOrdersPage _myOrdersPage = null!;

    [SetUp]
    public async Task SetUp()
    {
        _homePage = new HomePage(Page);
        _registerPage = new RegisterPage(Page);
        _loginPage = new LoginPage(Page);
        _productsPage = new ProductsPage(Page);
        _cartPage = new CartPage(Page);
        _checkoutPage = new CheckoutPage(Page);
        _myOrdersPage = new MyOrdersPage(Page);
        await Task.CompletedTask;
    }

    [Test, Order(1)]
    [Description("Complete E2E flow: New user registers, adds products, completes checkout with Stripe test card, and views order")]
    public async Task CompleteFlow_NewUser_CanRegisterShopAndCheckout()
    {
        // ========== STEP 1: REGISTER NEW USER ==========
        TestContext.WriteLine("Step 1: Registering new user...");
        var email = GenerateTestEmail();
        var username = $"shopper_{Guid.NewGuid():N}";
        var password = TestSettings.TestUserPassword;

        await _registerPage.NavigateAsync();
        await _registerPage.RegisterUserAsync(email, username, password, "E2E", "Shopper");
        await Page.WaitForTimeoutAsync(2000);

        // Navigate to login if not auto-logged in
        if (!await _homePage.IsLoggedInAsync())
        {
            await _loginPage.NavigateAsync();
            await _loginPage.LoginAsync(username, password);
            await Page.WaitForTimeoutAsync(2000);
        }

        var isLoggedIn = await _homePage.IsLoggedInAsync();
        Assert.That(isLoggedIn, Is.True, "User should be logged in after registration");
        TestContext.WriteLine("✓ User registered and logged in successfully");

        // ========== STEP 2: BROWSE AND ADD PRODUCTS TO CART ==========
        TestContext.WriteLine("Step 2: Browsing products and adding to cart...");
        await _productsPage.NavigateAsync();
        
        var productCount = await _productsPage.GetProductCountAsync();
        Assume.That(productCount, Is.GreaterThanOrEqualTo(2), "Need at least 2 products for this test");

        // Add 2 products to cart
        await _productsPage.AddToCartByIndexAsync(0);
        await Page.WaitForTimeoutAsync(1000);
        
        await _productsPage.AddToCartByIndexAsync(1);
        await Page.WaitForTimeoutAsync(1000);
        
        TestContext.WriteLine("✓ Added 2 products to cart");

        // ========== STEP 3: VERIFY CART ==========
        TestContext.WriteLine("Step 3: Verifying cart contents...");
        await _cartPage.NavigateAsync();
        
        var cartItemCount = await _cartPage.GetItemCountAsync();
        Assert.That(cartItemCount, Is.EqualTo(2), "Cart should contain 2 items");
        
        var isCheckoutEnabled = await _cartPage.IsCheckoutButtonEnabledAsync();
        Assert.That(isCheckoutEnabled, Is.True, "Checkout button should be enabled");
        
        TestContext.WriteLine($"✓ Cart verified: {cartItemCount} items");

        // ========== STEP 4: PROCEED TO CHECKOUT ==========
        TestContext.WriteLine("Step 4: Proceeding to checkout...");
        await _cartPage.GoToCheckoutAsync();
        await Page.WaitForTimeoutAsync(1000);

        // ========== STEP 5: COMPLETE CHECKOUT WITH STRIPE TEST CARD ==========
        TestContext.WriteLine("Step 5: Completing checkout with Stripe test card...");
        
        await _checkoutPage.CompleteCheckoutAsync(
            street: "123 Test Street",
            city: "Test City",
            state: "TX",
            postalCode: "12345",
            cardNumber: TestSettings.StripeTestCardSuccess,
            country: "USA",
            expiry: "12/30",
            cvc: "123"
        );

        // Wait for order processing (payment can take time)
        TestContext.WriteLine("Waiting for payment processing...");
        await Page.WaitForTimeoutAsync(5000);

        var isSuccess = await _checkoutPage.IsSuccessMessageVisibleAsync();
        
        if (!isSuccess)
        {
            // Check if there's an error
            var hasError = await _checkoutPage.IsErrorMessageVisibleAsync();
            if (hasError)
            {
                var errorMsg = await _checkoutPage.GetErrorMessageAsync();
                TestContext.WriteLine($"Checkout error: {errorMsg}");
                await TakeScreenshot("checkout_error");
            }
            
            // Take screenshot for debugging
            await TakeScreenshot("checkout_result");
        }

        Assert.That(isSuccess, Is.True, "Order should be placed successfully");
        
        var orderId = await _checkoutPage.GetOrderIdFromSuccessMessageAsync();
        TestContext.WriteLine($"✓ Order placed successfully. Order ID: {orderId}");

        // ========== STEP 6: VERIFY ORDER IN MY ORDERS ==========
        TestContext.WriteLine("Step 6: Verifying order in My Orders page...");
        await _myOrdersPage.NavigateAsync();
        
        var orderCount = await _myOrdersPage.GetOrderCountAsync();
        Assert.That(orderCount, Is.GreaterThan(0), "Should have at least one order");
        
        // Verify the order we just placed appears
        var recentOrderId = await _myOrdersPage.GetMostRecentOrderIdAsync();
        Assert.That(recentOrderId, Is.Not.Null.And.Not.Empty, "Should have a recent order");
        
        TestContext.WriteLine($"✓ Order appears in My Orders. Order ID: {recentOrderId}");
        TestContext.WriteLine("========== E2E TEST COMPLETED SUCCESSFULLY ==========");
    }

    [Test, Order(2)]
    [Description("Existing user can login, add to cart, and checkout")]
    public async Task CompleteFlow_ExistingUser_CanLoginShopAndCheckout()
    {
        // Use pre-seeded shopper account
        var username = "steve.hopper";
        var password = "P@ssw0rd";

        TestContext.WriteLine("Step 1: Logging in with existing user...");
        await _loginPage.NavigateAsync();
        await _loginPage.LoginAsync(username, password);
        await Page.WaitForTimeoutAsync(2000);

        var isLoggedIn = await _homePage.IsLoggedInAsync();
        Assert.That(isLoggedIn, Is.True, "User should be logged in");
        TestContext.WriteLine("✓ Logged in successfully");

        TestContext.WriteLine("Step 2: Adding product to cart...");
        await _productsPage.NavigateAsync();
        await _productsPage.AddToCartByIndexAsync(0);
        await Page.WaitForTimeoutAsync(1000);
        TestContext.WriteLine("✓ Product added to cart");

        TestContext.WriteLine("Step 3: Completing checkout...");
        await _cartPage.NavigateAsync();
        await _cartPage.GoToCheckoutAsync();

        await _checkoutPage.CompleteCheckoutAsync(
            street: "456 Main St",
            city: "Springfield",
            state: "IL",
            postalCode: "62701",
            cardNumber: TestSettings.StripeTestCardSuccess
        );

        await Page.WaitForTimeoutAsync(5000);

        var isSuccess = await _checkoutPage.IsSuccessMessageVisibleAsync();
        Assert.That(isSuccess, Is.True, "Order should be placed successfully");
        TestContext.WriteLine("✓ Order completed successfully");
    }

    [Test, Order(3)]
    [Description("Checkout fails with declined Stripe test card")]
    public async Task Checkout_FailsWithDeclinedCard()
    {
        // Setup - Login
        var username = "steve.hopper";
        var password = "P@ssw0rd";

        await _loginPage.NavigateAsync();
        await _loginPage.LoginAsync(username, password);
        await Page.WaitForTimeoutAsync(2000);

        // Add product and go to checkout
        await _productsPage.NavigateAsync();
        await _productsPage.AddToCartByIndexAsync(0);
        await _cartPage.NavigateAsync();
        await _cartPage.GoToCheckoutAsync();

        // Use declined test card
        TestContext.WriteLine("Testing with declined card...");
        await _checkoutPage.CompleteCheckoutAsync(
            street: "789 Test Ave",
            city: "Test City",
            state: "CA",
            postalCode: "90210",
            cardNumber: TestSettings.StripeTestCardDecline // 4000 0000 0000 0002
        );

        await Page.WaitForTimeoutAsync(5000);

        // Verify error is shown
        var hasError = await _checkoutPage.IsErrorMessageVisibleAsync();
        Assert.That(hasError, Is.True, "Should show error for declined card");

        var errorMsg = await _checkoutPage.GetErrorMessageAsync();
        TestContext.WriteLine($"Expected error received: {errorMsg}");
    }

    [Test, Order(4)]
    [Description("User can view order history with multiple orders")]
    public async Task User_CanViewOrderHistory()
    {
        // Login
        var username = "steve.hopper";
        var password = "P@ssw0rd";

        await _loginPage.NavigateAsync();
        await _loginPage.LoginAsync(username, password);
        await Page.WaitForTimeoutAsync(2000);

        // Navigate to My Orders
        await _myOrdersPage.NavigateAsync();

        var orderCount = await _myOrdersPage.GetOrderCountAsync();
        TestContext.WriteLine($"Total orders found: {orderCount}");

        // If user has orders, verify we can see them
        if (orderCount > 0)
        {
            var orderIds = await _myOrdersPage.GetAllOrderIdsAsync();
            Assert.That(orderIds, Is.Not.Empty, "Should be able to retrieve order IDs");
            
            TestContext.WriteLine($"Order IDs: {string.Join(", ", orderIds)}");

            // Click on first order to view details
            await _myOrdersPage.ViewOrderDetailsByIndexAsync(0);
            await Page.WaitForTimeoutAsync(2000);
            
            Assert.That(Page.Url, Does.Contain("/order"), "Should navigate to order details page");
            TestContext.WriteLine("✓ Can view order details");
        }
        else
        {
            TestContext.WriteLine("No orders found for this user");
        }
    }
}
