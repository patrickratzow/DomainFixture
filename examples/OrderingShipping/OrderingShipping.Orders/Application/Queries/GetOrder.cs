using OrderingShipping.Orders.Domain;
using OrderingShipping.SharedKernel;

namespace OrderingShipping.Orders.Application;

public sealed record GetOrder(OrderId OrderId) : IQuery<OrderDetails?>;
