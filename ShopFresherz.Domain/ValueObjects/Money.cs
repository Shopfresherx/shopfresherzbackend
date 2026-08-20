namespace ShopFresherz.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount in a specific currency.
/// Immutable value object — equality is by value, not reference.
/// </summary>
public sealed record Money(decimal Amount, string Currency = "NGN")
{
    /// <summary>Gets a zero-value NGN money instance.</summary>
    public static Money Zero => new Money(0m, "NGN");

    /// <summary>Adds two money values of the same currency.</summary>
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException($"Cannot add {left.Currency} and {right.Currency}.");
        }

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>Subtracts two money values of the same currency.</summary>
    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException($"Cannot subtract {left.Currency} from {right.Currency}.");
        }

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>Multiplies the amount by a scalar quantity.</summary>
    public static Money operator *(Money money, int quantity) => new Money(money.Amount * quantity, money.Currency);

    /// <summary>Returns a formatted display string, e.g., ₦45,000.00</summary>
    public override string ToString() => $"₦{Amount:N2}";
}
