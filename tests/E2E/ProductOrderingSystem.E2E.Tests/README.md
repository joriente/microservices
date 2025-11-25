# E2E Tests with Playwright

This project contains end-to-end tests for the Product Ordering System using Playwright and NUnit.

## Prerequisites

- .NET 9.0 SDK
- Playwright browsers installed (Chromium)
- All microservices running via Docker Compose or Aspire
- Frontend running on http://localhost:5173
- API Gateway running on http://localhost:8080
- Stripe test mode configured

## Installation

### Install Playwright Browsers

After building the project, install the required browsers:

```powershell
# From this directory
pwsh bin/Debug/net9.0/playwright.ps1 install chromium

# Or install all browsers
pwsh bin/Debug/net9.0/playwright.ps1 install
```

## Running Tests

### Run All Tests

```powershell
dotnet test
```

### Run Specific Test Suite

```powershell
# Anonymous browsing tests
dotnet test --filter "FullyQualifiedName~AnonymousBrowsingTests"

# User signup tests
dotnet test --filter "FullyQualifiedName~UserSignupTests"

# Complete shopping flow tests
dotnet test --filter "FullyQualifiedName~CompleteShoppingFlowTests"
```

### Run Specific Test

```powershell
dotnet test --filter "FullyQualifiedName~CompleteFlow_NewUser_CanRegisterShopAndCheckout"
```

### Run with Headed Browser

By default, tests run with the browser visible. To run headless:

```powershell
$env:E2E_HEADLESS="true"
dotnet test
```

### Run with Slow Motion

Useful for debugging - slows down operations:

```powershell
$env:E2E_SLOWMO="500"  # 500ms delay between operations
dotnet test
```

## Environment Variables

Configure test behavior via environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `E2E_BASE_URL` | Frontend base URL | `http://localhost:5173` |
| `E2E_API_BASE_URL` | API Gateway URL | `http://localhost:8080` |
| `E2E_HEADLESS` | Run browser in headless mode | `false` |
| `E2E_SLOWMO` | Slow down operations (milliseconds) | `0` |

Example:

```powershell
$env:E2E_BASE_URL="http://localhost:3000"
$env:E2E_HEADLESS="true"
dotnet test
```

## Test Coverage

### User Flows

✅ **Anonymous Browsing** (6 tests)
- View home page
- Browse products without login
- Search products
- View product details
- Redirect to login when adding to cart
- Navigate between pages

✅ **User Signup** (6 tests)
- Successful registration
- Duplicate email validation
- Invalid email format validation
- Weak password validation
- Navigate from register to login
- Immediate login after registration

✅ **Complete Shopping Flow** (4 tests)
- New user registration → browse → add to cart → checkout → view orders
- Existing user login → shop → checkout
- Payment with declined card
- View order history

### Stripe Test Cards

The following test cards are configured for payment testing:

| Card Number | Purpose | Expected Result |
|-------------|---------|-----------------|
| `4242 4242 4242 4242` | Success | Payment succeeds |
| `4000 0000 0000 0002` | Decline | Payment declined |
| `4000 0000 0009 9995` | Insufficient funds | Payment fails |

**Test Card Details:**
- Expiry: `12/30` (any future date)
- CVC: `123` (any 3 digits)
- ZIP: `12345` (any 5 digits)

## Test Output

### Videos

All tests record videos to `videos/` directory. Videos are saved regardless of test outcome.

```
videos/
  AnonymousUser_CanViewHomePage-20240101-120000.webm
  CompleteFlow_NewUser_CanRegisterShopAndCheckout-20240101-120100.webm
```

### Traces

Failed tests save traces to `traces/` directory for debugging. Passed tests do not save traces to save disk space.

```
traces/
  CompleteFlow_NewUser_CanRegisterShopAndCheckout-20240101-120100.zip
```

**View trace files:**

```powershell
npx playwright show-trace traces/[trace-file].zip
```

### Screenshots

Use the `TakeScreenshot()` helper method in tests to capture debug screenshots:

```csharp
await TakeScreenshot("before-checkout");
```

Screenshots are saved to `screenshots/` directory.

## Test Data

### Pre-seeded Users

The following user exists in the system for testing:

- Username: `steve.hopper`
- Password: `P@ssw0rd`
- Email: `steve.hopper@email.com`

### Generated Test Users

Tests that require new users generate unique emails:

```
testuser_7f8d4e3a2b1c9d6e5f4a3b2c1d0e9f8a@playwright.test
```

This allows tests to run in parallel without conflicts.

## Architecture

### Page Object Pattern

Tests use the Page Object Model pattern for maintainability:

```
PageObjects/
  HomePage.cs          - Home page navigation
  RegisterPage.cs      - User registration
  LoginPage.cs         - Authentication
  ProductsPage.cs      - Product catalog
  CartPage.cs          - Shopping cart
  CheckoutPage.cs      - Checkout and payment
  MyOrdersPage.cs      - Order history
```

### Base Test Class

`PlaywrightTest.cs` provides common functionality:
- Browser context configuration (viewport, video, tracing)
- Setup/teardown with automatic tracing
- Helper methods (page load, screenshots, test email generation)

### Configuration

`Configuration/TestSettings.cs` centralizes test configuration with environment variable support.

## Troubleshooting

### Connection Errors

**Symptom:** Tests fail with "Connection refused" or timeout errors

**Solution:**
1. Ensure all microservices are running:
   ```powershell
   cd src\Aspire\ProductOrderingSystem.AppHost
   dotnet run
   ```
2. Verify frontend is accessible: http://localhost:5173
3. Verify gateway is accessible: http://localhost:8080

### Stripe Payment Failures

**Symptom:** Checkout tests fail with payment errors

**Solution:**
1. Ensure Stripe is configured in test mode
2. Verify test cards are correctly configured in `TestSettings.cs`
3. Check Stripe iframe loading (may need increased timeout)

### Browser Not Found

**Symptom:** `Browser executable doesn't exist` error

**Solution:**
```powershell
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

### Timeout Errors

**Symptom:** Tests fail with "Timeout 30000ms exceeded" errors

**Solution:**
1. Increase timeout in `TestSettings.cs` if needed
2. Check if services are slow to respond
3. Use `E2E_SLOWMO` to slow down operations and observe issues

### Trace Viewing

View detailed trace for failed tests:

```powershell
# Install Playwright CLI if not already installed
npm install -g playwright

# View trace
npx playwright show-trace traces/your-test-trace.zip
```

The trace viewer shows:
- Screenshots at each step
- Network requests
- Console logs
- DOM snapshots
- Source code

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Install Playwright Browsers
  run: pwsh tests/E2E/ProductOrderingSystem.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install chromium

- name: Run E2E Tests
  run: dotnet test tests/E2E/ProductOrderingSystem.E2E.Tests
  env:
    E2E_HEADLESS: true
    E2E_BASE_URL: ${{ env.FRONTEND_URL }}
    E2E_API_BASE_URL: ${{ env.API_URL }}

- name: Upload Test Results
  if: failure()
  uses: actions/upload-artifact@v3
  with:
    name: playwright-traces
    path: tests/E2E/ProductOrderingSystem.E2E.Tests/traces/
```

### Best Practices for CI

- Set `E2E_HEADLESS=true` in CI environments
- Store traces and videos as artifacts for failed tests
- Run against staging/test environment, not production
- Consider running on schedule (nightly) due to test duration
- Use test parallelization for faster execution

## Development

### Adding New Tests

1. Create a new test class inheriting from `PlaywrightTest`:
   ```csharp
   [TestFixture]
   public class MyNewTests : PlaywrightTest
   {
       [Test]
       public async Task MyNewTest()
       {
           // Use Page object from base class
           await Page.GotoAsync(TestSettings.BaseUrl);
           // ...
       }
   }
   ```

2. Use existing page objects or create new ones:
   ```csharp
   var homePage = new HomePage(Page);
   await homePage.NavigateAsync();
   ```

3. Use helper methods from base class:
   ```csharp
   await WaitForPageLoad();
   await TakeScreenshot("debug-point");
   string email = GenerateTestEmail();
   ```

### Creating New Page Objects

```csharp
public class MyPage
{
    private readonly IPage _page;
    
    public MyPage(IPage page)
    {
        _page = page;
    }
    
    // Locators
    private ILocator MyButton => _page.GetByRole(AriaRole.Button, new() { Name = "My Button" });
    
    // Actions
    public async Task ClickMyButtonAsync()
    {
        await MyButton.ClickAsync();
    }
}
```

## Resources

- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [NUnit Documentation](https://docs.nunit.org/)
- [Stripe Testing Documentation](https://stripe.com/docs/testing)
