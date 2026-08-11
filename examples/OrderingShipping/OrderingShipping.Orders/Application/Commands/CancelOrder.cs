using OrderingShipping.Orders.Domain;
using OrderingShipping.SharedKernel;

namespace OrderingShipping.Orders.Application;

public sealed record CancelOrder(OrderId OrderId, string Reason) : ICommand<bool>;
