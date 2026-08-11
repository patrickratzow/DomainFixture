using OrderingShipping.Orders.Domain;

namespace OrderingShipping.Orders.Application;

public sealed record OrderDetails(
    OrderId OrderId,
    CustomerId CustomerId,
    OrderStatus Status,
    decimal Total,
    string Currency,
    int LineCount);
