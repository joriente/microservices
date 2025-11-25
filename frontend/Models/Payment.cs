namespace ProductOrderingSystem.Web.Models;

public record PaymentRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string PaymentMethod,
    CardDetails? CardDetails);

public record CardDetails(
    string CardNumber,
    string CardHolderName,
    string ExpiryMonth,
    string ExpiryYear,
    string Cvv);

public record PaymentResponse(
    Guid PaymentId,
    Guid OrderId,
    string Status,
    string? Message);

public record ProcessPaymentRequest(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency);

// Stripe test card numbers
public static class StripeTestCards
{
    public const string SuccessVisa = "4242424242424242";
    public const string SuccessMastercard = "5555555555554444";
    public const string DeclineCard = "4000000000000002";
    public const string InsufficientFunds = "4000000000009995";
    public const string ExpiredCard = "4000000000000069";
    public const string IncorrectCVC = "4000000000000127";
    
    public static readonly Dictionary<string, string> TestCardDescriptions = new()
    {
        { SuccessVisa, "Visa - Success" },
        { SuccessMastercard, "Mastercard - Success" },
        { DeclineCard, "Generic Decline" },
        { InsufficientFunds, "Insufficient Funds" },
        { ExpiredCard, "Expired Card" },
        { IncorrectCVC, "Incorrect CVC" }
    };
}
