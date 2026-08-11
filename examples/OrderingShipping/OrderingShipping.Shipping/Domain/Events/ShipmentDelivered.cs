using OrderingShipping.SharedKernel;

namespace OrderingShipping.Shipping.Domain;

public sealed record ShipmentDelivered(
    ShipmentId ShipmentId,
    Guid OrderId) : IDomainEvent;
