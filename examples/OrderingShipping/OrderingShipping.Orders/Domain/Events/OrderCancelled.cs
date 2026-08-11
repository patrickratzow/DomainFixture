using OrderingShipping.SharedKernel;

namespace OrderingShipping.Orders.Domain;

public sealed record OrderCancelled(
    OrderId OrderId,
    string Reason) : IDomainEvent;
