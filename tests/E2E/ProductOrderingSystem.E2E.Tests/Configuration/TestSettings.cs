namespace ProductOrderingSystem.E2E.Tests.Configuration;

public static class TestSettings
{
    // Base URLs - Update these based on your environment
    public static string BaseUrl => Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5200";
    public static string ApiBaseUrl => Environment.GetEnvironmentVariable("E2E_API_BASE_URL") ?? "http://localhost:5000";
    
    // Test user credentials
    public static string TestUserEmail => $"testuser_{Guid.NewGuid():N}@playwright.test";
    public static string TestUserPassword => "Test@1234";
    public static string TestUserFirstName => "Playwright";
    public static string TestUserLastName => "Tester";
    
    // Stripe test card numbers
    public static string StripeTestCardSuccess => "4242424242424242";
    public static string StripeTestCardDecline => "4000000000000002";
    public static string StripeTestCardInsufficientFunds => "4000000000009995";
    
    // Test timeouts
    public static int DefaultTimeout => 30000; // 30 seconds
    public static int LongTimeout => 60000; // 60 seconds for checkout operations
    
    // Browser settings
    public static bool Headless => bool.Parse(Environment.GetEnvironmentVariable("E2E_HEADLESS") ?? "false");
    public static float SlowMo => float.Parse(Environment.GetEnvironmentVariable("E2E_SLOWMO") ?? "0");
}
