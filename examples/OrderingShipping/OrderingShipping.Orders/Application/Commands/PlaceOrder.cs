using OrderingShipping.Orders.Domain;
using OrderingShipping.SharedKernel;

namespace OrderingShipping.Orders.Application;

public sealed record PlaceOrder(
    OrderId OrderId,
    CustomerId CustomerId,
    ShippingAddress ShipTo,
    IReadOnlyList<OrderLine> Lines) : ICommand<OrderId>;
