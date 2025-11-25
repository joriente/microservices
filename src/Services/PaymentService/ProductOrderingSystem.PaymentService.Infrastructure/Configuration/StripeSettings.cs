namespace ProductOrderingSystem.PaymentService.Infrastructure.Configuration;

public class StripeSettings
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
