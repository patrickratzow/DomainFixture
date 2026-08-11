using OrderingShipping.SharedKernel;

namespace OrderingShipping.Shipping.Domain;

public sealed record ShipmentDispatched(
    ShipmentId ShipmentId,
    Guid OrderId,
    TrackingNumber TrackingNumber) : IDomainEvent;
