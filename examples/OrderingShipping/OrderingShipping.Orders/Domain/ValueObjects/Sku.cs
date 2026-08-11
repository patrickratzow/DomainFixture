namespace OrderingShipping.Orders.Domain;

public readonly record struct Sku(string Value)
{
    public static Sku From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A SKU is required.", nameof(value));
        }

        return new Sku(value.Trim().ToUpperInvariant());
    }
}
