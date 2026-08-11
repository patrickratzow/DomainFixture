using OrderingShipping.SharedKernel;

namespace OrderingShipping.Orders.Domain;

public sealed record OrderPlaced(
    OrderId OrderId,
    CustomerId CustomerId,
    ShippingAddressSnapshot ShipTo,
    IReadOnlyList<OrderLineSnapshot> Lines,
    decimal Total,
    string Currency) : IDomainEvent;
