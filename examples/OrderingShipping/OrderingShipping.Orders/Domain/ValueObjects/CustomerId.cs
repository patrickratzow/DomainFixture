namespace OrderingShipping.Orders.Domain;

public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId From(Guid value) => value == Guid.Empty
        ? throw new ArgumentException("A customer id cannot be empty.", nameof(value))
        : new CustomerId(value);
}
