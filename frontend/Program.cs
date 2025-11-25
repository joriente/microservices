using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using Blazored.LocalStorage;
using ProductOrderingSystem.Web;
using ProductOrderingSystem.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API Gateway base URL
// When running in Aspire, use service discovery name "http://api-gateway"
// Otherwise, use localhost URL from configuration
var apiBaseUrl = builder.Configuration["ApiGateway:BaseUrl"] ?? "http://localhost:5555";

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register HTTP clients for microservices
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Register API client services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Register authentication state provider
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
    sp.GetRequiredService<CustomAuthStateProvider>());

await builder.Build().RunAsync();
