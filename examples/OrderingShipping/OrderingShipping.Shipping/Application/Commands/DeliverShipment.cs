using OrderingShipping.SharedKernel;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.Shipping.Application;

public sealed record DeliverShipment(ShipmentId ShipmentId) : ICommand<bool>;
