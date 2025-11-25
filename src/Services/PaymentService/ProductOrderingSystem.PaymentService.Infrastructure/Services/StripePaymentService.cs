using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductOrderingSystem.PaymentService.Domain.Services;
using ProductOrderingSystem.PaymentService.Infrastructure.Configuration;
using Stripe;

namespace ProductOrderingSystem.PaymentService.Infrastructure.Services;

public class StripePaymentService : IStripePaymentService
{
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IOptions<StripeSettings> settings,
        ILogger<StripePaymentService> logger)
    {
        StripeConfiguration.ApiKey = settings.Value.SecretKey;
        _paymentIntentService = new PaymentIntentService();
        _refundService = new RefundService();
        _logger = logger;
    }

    public async Task<ErrorOr<string>> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Stripe expects amounts in smallest currency unit (cents for USD)
            var amountInCents = (long)(amount * 100);

            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = currency.ToLowerInvariant(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never" // Disable redirect-based payment methods for POC
                },
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", orderId.ToString() },
                    { "user_id", userId.ToString() }
                }
            };

            var paymentIntent = await _paymentIntentService.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Created Stripe PaymentIntent {PaymentIntentId} for Order {OrderId}, Amount: {Amount} {Currency}",
                paymentIntent.Id,
                orderId,
                amount,
                currency);

            return paymentIntent.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe error creating payment intent for Order {OrderId}: {Error}",
                orderId,
                ex.Message);
            return Error.Failure("Stripe.CreatePaymentIntent", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error creating payment intent for Order {OrderId}",
                orderId);
            return Error.Failure("Stripe.CreatePaymentIntent", "An unexpected error occurred");
        }
    }

    public async Task<ErrorOr<Success>> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new PaymentIntentConfirmOptions
            {
                // For POC: Use test card payment method
                PaymentMethod = "pm_card_visa" // Stripe test payment method
            };

            var paymentIntent = await _paymentIntentService.ConfirmAsync(
                paymentIntentId,
                options,
                cancellationToken: cancellationToken);

            if (paymentIntent.Status == "succeeded")
            {
                _logger.LogInformation(
                    "Successfully confirmed PaymentIntent {PaymentIntentId}",
                    paymentIntentId);
                return Result.Success;
            }
            else
            {
                _logger.LogWarning(
                    "PaymentIntent {PaymentIntentId} confirmation resulted in status: {Status}",
                    paymentIntentId,
                    paymentIntent.Status);
                return Error.Failure("Stripe.ConfirmPayment", $"Payment status: {paymentIntent.Status}");
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe error confirming payment intent {PaymentIntentId}: {Error}",
                paymentIntentId,
                ex.Message);
            return Error.Failure("Stripe.ConfirmPayment", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error confirming payment intent {PaymentIntentId}",
                paymentIntentId);
            return Error.Failure("Stripe.ConfirmPayment", "An unexpected error occurred");
        }
    }

    public async Task<ErrorOr<Success>> RefundPaymentAsync(
        string paymentIntentId,
        decimal amount,
        string currency,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var amountInCents = (long)(amount * 100);

            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = amountInCents,
                Reason = reason switch
                {
                    _ when reason.Contains("duplicate", StringComparison.OrdinalIgnoreCase) => "duplicate",
                    _ when reason.Contains("fraud", StringComparison.OrdinalIgnoreCase) => "fraudulent",
                    _ => "requested_by_customer"
                },
                Metadata = new Dictionary<string, string>
                {
                    { "reason_detail", reason }
                }
            };

            var refund = await _refundService.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Created refund {RefundId} for PaymentIntent {PaymentIntentId}, Amount: {Amount} {Currency}",
                refund.Id,
                paymentIntentId,
                amount,
                currency);

            return Result.Success;
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe error creating refund for PaymentIntent {PaymentIntentId}: {Error}",
                paymentIntentId,
                ex.Message);
            return Error.Failure("Stripe.Refund", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error creating refund for PaymentIntent {PaymentIntentId}",
                paymentIntentId);
            return Error.Failure("Stripe.Refund", "An unexpected error occurred");
        }
    }

    public async Task<ErrorOr<string>> GetPaymentStatusAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(
                paymentIntentId,
                cancellationToken: cancellationToken);

            return paymentIntent.Status;
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe error getting payment intent {PaymentIntentId}: {Error}",
                paymentIntentId,
                ex.Message);
            return Error.Failure("Stripe.GetStatus", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error getting payment intent {PaymentIntentId}",
                paymentIntentId);
            return Error.Failure("Stripe.GetStatus", "An unexpected error occurred");
        }
    }
}
