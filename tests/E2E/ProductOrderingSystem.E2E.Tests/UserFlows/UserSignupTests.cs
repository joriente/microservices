using ProductOrderingSystem.E2E.Tests.Configuration;
using ProductOrderingSystem.E2E.Tests.PageObjects;

namespace ProductOrderingSystem.E2E.Tests.UserFlows;

[TestFixture]
[Description("Tests for user registration and signup flow")]
public class UserSignupTests : PlaywrightTest
{
    private HomePage _homePage = null!;
    private RegisterPage _registerPage = null!;
    private LoginPage _loginPage = null!;

    [SetUp]
    public async Task SetUp()
    {
        _homePage = new HomePage(Page);
        _registerPage = new RegisterPage(Page);
        _loginPage = new LoginPage(Page);
        await Task.CompletedTask;
    }

    [Test]
    [Description("New user can successfully register an account")]
    public async Task NewUser_CanRegisterSuccessfully()
    {
        // Arrange
        var email = GenerateTestEmail();
        var username = $"testuser_{Guid.NewGuid():N}";
        var password = TestSettings.TestUserPassword;
        var firstName = TestSettings.TestUserFirstName;
        var lastName = TestSettings.TestUserLastName;

        // Act
        await _registerPage.NavigateAsync();
        await _registerPage.RegisterUserAsync(email, username, password, firstName, lastName);

        // Assert - After successful registration, should navigate away from /register page
        var currentUrl = Page.Url;
        var isError = await _registerPage.IsErrorMessageVisibleAsync();
        
        Assert.That(isError, Is.False, "Registration should not show error message");
        Assert.That(currentUrl.Contains("/register"), Is.False, 
            "Should navigate away from register page after successful registration");

        // Should be logged in after registration
        var isLoggedIn = await _homePage.IsLoggedInAsync();
        Assert.That(isLoggedIn, Is.True, "Should be logged in after successful registration");
    }

    [Test]
    [Description("Registration fails with duplicate email")]
    public async Task Registration_FailsWithDuplicateEmail()
    {
        // Arrange - Register first user
        var email = GenerateTestEmail();
        var username1 = $"user1_{Guid.NewGuid():N}";
        var username2 = $"user2_{Guid.NewGuid():N}";
        var password = TestSettings.TestUserPassword;

        await _registerPage.NavigateAsync();
        await _registerPage.RegisterUserAsync(
            email, username1, password, "First", "User");
        
        await Page.WaitForTimeoutAsync(2000);

        // Act - Try to register with same email
        await _registerPage.NavigateAsync();
        await _registerPage.RegisterUserAsync(
            email, username2, password, "Second", "User");

        await Page.WaitForTimeoutAsync(2000);

        // Assert
        var hasError = await _registerPage.IsErrorMessageVisibleAsync();
        Assert.That(hasError, Is.True, "Should show error for duplicate email");

        var errorMessage = await _registerPage.GetErrorMessageAsync();
        Assert.That(errorMessage.ToLower(), Does.Contain("email").Or.Contain("exists").Or.Contain("already"), 
            "Error message should mention email already exists");
    }

    [Test]
    [Description("Registration fails with invalid email format")]
    public async Task Registration_FailsWithInvalidEmail()
    {
        // Arrange
        var invalidEmail = "not-an-email";
        var username = $"testuser_{Guid.NewGuid():N}";
        var password = TestSettings.TestUserPassword;

        // Act
        await _registerPage.NavigateAsync();
        await _registerPage.RegisterUserAsync(
            invalidEmail, username, password, "Test", "User");

        await Page.WaitForTimeoutAsync(1000);

        // Assert - Either error message or HTML5 validation prevents submission
        var hasError = await _registerPage.IsErrorMessageVisibleAsync();
        var emailInput = Page.GetByLabel("Email");
        var validationMessage = await emailInput.EvaluateAsync<string>("el => el.validationMessage");

        Assert.That(hasError || !string.IsNullOrEmpty(validationMessage), Is.True, 
            "Should show validation error for invalid email");
    }

    [Test]
    [Description("Registration fails with weak password")]
    public async Task Registration_FailsWithWeakPassword()
    {
        // Arrange
        var email = GenerateTestEmail();
        var username = $"testuser_{Guid.NewGuid():N}";
        var weakPassword = "123"; // Too weak

        // Act
        await _registerPage.NavigateAsync();
        await _registerPage.RegisterUserAsync(
            email, username, weakPassword, "Test", "User");

        await Page.WaitForTimeoutAsync(1000);

        // Assert
        var hasError = await _registerPage.IsErrorMessageVisibleAsync();
        var passwordInput = Page.Locator("input[type='password']").First;
        var validationMessage = await passwordInput.EvaluateAsync<string>("el => el.validationMessage");

        Assert.That(hasError || !string.IsNullOrEmpty(validationMessage), Is.True, 
            "Should show validation error for weak password");
    }

    [Test]
    [Description("User can navigate from register to login page")]
    public async Task User_CanNavigateFromRegisterToLogin()
    {
        // Arrange
        await _registerPage.NavigateAsync();

        // Act
        await _registerPage.GoToLoginAsync();

        // Assert
        Assert.That(Page.Url, Does.Contain("/login"), "Should navigate to login page");
    }

    [Test]
    [Description("Registered user can immediately log in")]
    public async Task RegisteredUser_CanImmediatelyLogin()
    {
        // Arrange - Register new user
        var email = GenerateTestEmail();
        var username = $"testuser_{Guid.NewGuid():N}";
        var password = TestSettings.TestUserPassword;

        await _registerPage.NavigateAsync();
        await _registerPage.RegisterUserAsync(
            email, username, password, "Test", "User");

        await Page.WaitForTimeoutAsync(2000);

        // Act - Navigate to login and login
        await _loginPage.NavigateAsync();
        await _loginPage.LoginAsync(username, password);

        await Page.WaitForTimeoutAsync(2000);

        // Assert
        var isLoggedIn = await _homePage.IsLoggedInAsync();
        Assert.That(isLoggedIn, Is.True, "Should be logged in after registration and login");
    }
}
