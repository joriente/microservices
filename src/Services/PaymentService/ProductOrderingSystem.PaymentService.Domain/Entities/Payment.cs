using ErrorOr;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ProductOrderingSystem.PaymentService.Domain.Enums;
using ProductOrderingSystem.PaymentService.Domain.Events;
using ProductOrderingSystem.PaymentService.Domain.ValueObjects;

namespace ProductOrderingSystem.PaymentService.Domain.Entities;

public class Payment
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; private set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid OrderId { get; private set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; private set; }
    
    public Money Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<object> _domainEvents = new();
    
    [BsonIgnore]
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private Payment() 
    {
        Amount = default!; // EF Core will set this
    }

    private Payment(
        Guid id,
        Guid orderId,
        Guid userId,
        Money amount)
    {
        Id = id;
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static ErrorOr<Payment> Create(Guid orderId, Guid userId, Money amount)
    {
        if (orderId == Guid.Empty)
        {
            return Error.Validation("Payment.InvalidOrderId", "Order ID cannot be empty");
        }

        if (userId == Guid.Empty)
        {
            return Error.Validation("Payment.InvalidUserId", "User ID cannot be empty");
        }

        var payment = new Payment(Guid.NewGuid(), orderId, userId, amount);
        
        payment._domainEvents.Add(new PaymentCreatedEvent(
            payment.Id,
            payment.OrderId,
            payment.UserId,
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.CreatedAt));

        return payment;
    }

    public ErrorOr<Success> MarkAsProcessing(string stripePaymentIntentId)
    {
        if (Status != PaymentStatus.Pending)
        {
            return Error.Validation("Payment.InvalidStatus", $"Cannot mark payment as processing when status is {Status}");
        }

        if (string.IsNullOrWhiteSpace(stripePaymentIntentId))
        {
            return Error.Validation("Payment.InvalidStripeId", "Stripe payment intent ID is required");
        }

        Status = PaymentStatus.Processing;
        StripePaymentIntentId = stripePaymentIntentId;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success;
    }

    public ErrorOr<Success> MarkAsCompleted()
    {
        if (Status != PaymentStatus.Processing)
        {
            return Error.Validation("Payment.InvalidStatus", $"Cannot mark payment as completed when status is {Status}");
        }

        Status = PaymentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new PaymentProcessedEvent(
            Id,
            OrderId,
            StripePaymentIntentId!,
            Amount.Amount,
            Amount.Currency,
            CompletedAt.Value));

        return Result.Success;
    }

    public ErrorOr<Success> MarkAsFailed(string reason)
    {
        if (Status is PaymentStatus.Completed or PaymentStatus.Refunded)
        {
            return Error.Validation("Payment.InvalidStatus", $"Cannot mark payment as failed when status is {Status}");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation("Payment.InvalidReason", "Failure reason is required");
        }

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new PaymentFailedEvent(
            Id,
            OrderId,
            reason,
            DateTime.UtcNow));

        return Result.Success;
    }

    public ErrorOr<Success> MarkAsRefunded(string reason)
    {
        if (Status != PaymentStatus.Completed)
        {
            return Error.Validation("Payment.InvalidStatus", $"Cannot refund payment when status is {Status}");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation("Payment.InvalidReason", "Refund reason is required");
        }

        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new PaymentRefundedEvent(
            Id,
            OrderId,
            Amount.Amount,
            Amount.Currency,
            reason,
            DateTime.UtcNow));

        return Result.Success;
    }

    public ErrorOr<Success> MarkAsCancelled()
    {
        if (Status is not PaymentStatus.Pending and not PaymentStatus.Processing)
        {
            return Error.Validation("Payment.InvalidStatus", $"Cannot cancel payment when status is {Status}");
        }

        Status = PaymentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
