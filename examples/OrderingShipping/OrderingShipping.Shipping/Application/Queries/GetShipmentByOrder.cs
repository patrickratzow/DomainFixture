using OrderingShipping.SharedKernel;

namespace OrderingShipping.Shipping.Application;

public sealed record GetShipmentByOrder(Guid OrderId) : IQuery<ShipmentDetails?>;
