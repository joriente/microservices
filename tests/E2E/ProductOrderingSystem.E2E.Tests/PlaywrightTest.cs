using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using ProductOrderingSystem.E2E.Tests.Configuration;

namespace ProductOrderingSystem.E2E.Tests;

[TestFixture]
public class PlaywrightTest : PageTest
{
    protected string BaseUrl => TestSettings.BaseUrl;
    protected string ApiBaseUrl => TestSettings.ApiBaseUrl;

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true,
            Locale = "en-US",
            TimezoneId = "America/New_York",
            RecordVideoDir = "videos/",
            RecordVideoSize = new RecordVideoSize { Width = 1920, Height = 1080 }
        };
    }

    /// <summary>
    /// Helper method to wait for page to be fully loaded
    /// </summary>
    protected async Task WaitForPageLoad()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    /// <summary>
    /// Helper method to take a screenshot (useful for debugging)
    /// </summary>
    protected async Task TakeScreenshot(string name)
    {
        var screenshotPath = $"screenshots/{TestContext.CurrentContext.Test.Name}_{name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
        await Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
    }

    /// <summary>
    /// Helper to generate unique test email
    /// </summary>
    protected string GenerateTestEmail() => $"testuser_{Guid.NewGuid():N}@playwright.test";
}
