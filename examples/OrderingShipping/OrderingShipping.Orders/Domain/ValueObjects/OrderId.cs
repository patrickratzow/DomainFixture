namespace OrderingShipping.Orders.Domain;

public readonly record struct OrderId(Guid Value)
{
    public static OrderId From(Guid value) => value == Guid.Empty
        ? throw new ArgumentException("An order id cannot be empty.", nameof(value))
        : new OrderId(value);
}
