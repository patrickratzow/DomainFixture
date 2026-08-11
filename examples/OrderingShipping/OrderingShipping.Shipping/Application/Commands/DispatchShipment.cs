using OrderingShipping.SharedKernel;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.Shipping.Application;

public sealed record DispatchShipment(
    ShipmentId ShipmentId,
    TrackingNumber TrackingNumber) : ICommand<bool>;
