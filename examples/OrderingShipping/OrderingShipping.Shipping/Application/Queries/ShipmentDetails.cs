using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.Shipping.Application;

public sealed record ShipmentDetails(
    ShipmentId ShipmentId,
    Guid OrderId,
    ShipmentStatus Status,
    string? TrackingNumber);
