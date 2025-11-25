using ErrorOr;

namespace ProductOrderingSystem.PaymentService.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static ErrorOr<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
        {
            return Error.Validation("Money.InvalidAmount", "Amount cannot be negative");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Error.Validation("Money.InvalidCurrency", "Currency is required");
        }

        if (currency.Length != 3)
        {
            return Error.Validation("Money.InvalidCurrency", "Currency must be a 3-letter ISO code");
        }

        return new Money(amount, currency.ToUpperInvariant());
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot add money with different currencies");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot subtract money with different currencies");
        }

        return new Money(Amount - other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
