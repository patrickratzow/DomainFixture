namespace OrderingShipping.Orders.Domain;

public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Usd(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Money cannot be negative.");
        }

        return new Money(amount, "USD");
    }

    public Money Multiply(int quantity) => new(Amount * quantity, Currency);
}
