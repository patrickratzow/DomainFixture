namespace OrderingShipping.Shipping.Domain;

public readonly record struct TrackingNumber(string Value)
{
    public static TrackingNumber From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A tracking number is required.", nameof(value));
        }

        return new TrackingNumber(value.Trim().ToUpperInvariant());
    }
}
