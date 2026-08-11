using OrderingShipping.SharedKernel;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.Shipping.Application;

public sealed record CreateShipment(
    ShipmentId ShipmentId,
    Guid OrderId,
    DeliveryAddress Destination) : ICommand<ShipmentId>;
