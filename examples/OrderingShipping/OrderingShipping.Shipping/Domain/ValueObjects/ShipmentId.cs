namespace OrderingShipping.Shipping.Domain;

public readonly record struct ShipmentId(Guid Value)
{
    public static ShipmentId From(Guid value) => value == Guid.Empty
        ? throw new ArgumentException("A shipment id cannot be empty.", nameof(value))
        : new ShipmentId(value);
}
