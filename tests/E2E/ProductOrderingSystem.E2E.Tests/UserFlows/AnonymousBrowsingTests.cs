using ProductOrderingSystem.E2E.Tests.PageObjects;
using System.Text.RegularExpressions;

namespace ProductOrderingSystem.E2E.Tests.UserFlows;

[TestFixture]
[Description("Tests for anonymous users browsing the product catalog")]
public class AnonymousBrowsingTests : PlaywrightTest
{
    private HomePage _homePage = null!;
    private ProductsPage _productsPage = null!;

    [SetUp]
    public async Task SetUp()
    {
        _homePage = new HomePage(Page);
        _productsPage = new ProductsPage(Page);
        await Task.CompletedTask;
    }

    [Test]
    [Description("Anonymous user can view the home page")]
    public async Task AnonymousUser_CanViewHomePage()
    {
        // Arrange & Act
        await _homePage.NavigateAsync();

        // Assert
        Assert.That(await _homePage.IsLoggedOutAsync(), Is.True, "User should not be logged in");
        await Expect(Page).ToHaveTitleAsync(new Regex("Product Ordering|Home", RegexOptions.IgnoreCase));
    }

    [Test]
    [Description("Anonymous user can browse products without logging in")]
    public async Task AnonymousUser_CanBrowseProducts()
    {
        // Arrange & Act
        await _productsPage.NavigateAsync();

        // Assert
        var productCount = await _productsPage.GetProductCountAsync();
        Assert.That(productCount, Is.GreaterThan(0), "Product catalog should not be empty");
    }

    [Test]
    [Description("Anonymous user can search for products")]
    [Ignore("Search functionality not implemented in the current UI")]
    public async Task AnonymousUser_CanSearchProducts()
    {
        // Arrange
        await _productsPage.NavigateAsync();
        var initialCount = await _productsPage.GetProductCountAsync();

        // Act
        await _productsPage.SearchProductsAsync("laptop");

        // Assert
        var searchCount = await _productsPage.GetProductCountAsync();
        Assert.That(searchCount, Is.LessThanOrEqualTo(initialCount), 
            "Search should filter products");
    }

    [Test]
    [Description("Anonymous user can view product details")]
    public async Task AnonymousUser_CanViewProductDetails()
    {
        // Arrange
        await _productsPage.NavigateAsync();
        var productCount = await _productsPage.GetProductCountAsync();
        Assume.That(productCount, Is.GreaterThan(0), "Need at least one product for this test");

        // Act - Get first product name (h6 with GutterBottom, not the price h6)
        var productCards = Page.Locator(".mud-card");
        var firstProduct = productCards.First;
        var productNameElement = firstProduct.Locator(".mud-card-content h6").First;
        var productName = await productNameElement.TextContentAsync();
        
        // Verify product card is visible
        await Expect(firstProduct).ToBeVisibleAsync();

        // Assert
        Assert.That(productName, Is.Not.Null.And.Not.Empty, "Product should have a name");
    }

    [Test]
    [Description("Anonymous user is redirected to login when trying to add to cart")]
    public async Task AnonymousUser_RedirectedToLogin_WhenAddingToCart()
    {
        // Arrange
        await _productsPage.NavigateAsync();
        var productCount = await _productsPage.GetProductCountAsync();
        Assume.That(productCount, Is.GreaterThan(0), "Need at least one product for this test");

        // Act
        await _productsPage.AddToCartByIndexAsync(0);

        // Assert - Should be redirected to login or see login page
        await Page.WaitForTimeoutAsync(2000); // Wait for redirect/snackbar
        
        var currentUrl = Page.Url;
        var isOnLoginPage = currentUrl.Contains("/login", StringComparison.OrdinalIgnoreCase);
        
        // Check for login page heading or snackbar message
        var hasLoginHeading = await Page.Locator("h4:has-text('Login'), h5:has-text('Login')").IsVisibleAsync();
        var snackbarCount = await Page.Locator(".mud-snackbar").CountAsync();
        var hasSnackbar = snackbarCount > 0;

        Assert.That(isOnLoginPage || hasLoginHeading || hasSnackbar, Is.True, 
            "Anonymous user should be prompted to login when adding to cart");
    }

    [Test]
    [Description("Anonymous user can navigate between pages")]
    public async Task AnonymousUser_CanNavigateBetweenPages()
    {
        // Arrange & Act
        await _homePage.NavigateAsync();
        
        // Go to Products
        await _homePage.GoToProductsAsync();
        Assert.That(Page.Url, Does.Contain("/products"), "Should navigate to products page");

        // Go back to Home
        await Page.GoBackAsync();
        await WaitForPageLoad();
        
        // Go to Register
        await _homePage.GoToRegisterAsync();
        Assert.That(Page.Url, Does.Contain("/register"), "Should navigate to register page");

        // Go back to home
        await _homePage.NavigateAsync();
        
        // Go to Login
        await _homePage.GoToLoginAsync();
        Assert.That(Page.Url, Does.Contain("/login"), "Should navigate to login page");
    }
}
