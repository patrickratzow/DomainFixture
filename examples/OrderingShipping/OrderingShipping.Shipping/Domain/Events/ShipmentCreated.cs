using OrderingShipping.SharedKernel;

namespace OrderingShipping.Shipping.Domain;

public sealed record ShipmentCreated(
    ShipmentId ShipmentId,
    Guid OrderId) : IDomainEvent;
