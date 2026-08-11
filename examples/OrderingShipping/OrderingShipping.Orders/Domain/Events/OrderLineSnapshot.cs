namespace OrderingShipping.Orders.Domain;

public sealed record OrderLineSnapshot(
    string Sku,
    int Quantity,
    decimal UnitPrice,
    string Currency);
