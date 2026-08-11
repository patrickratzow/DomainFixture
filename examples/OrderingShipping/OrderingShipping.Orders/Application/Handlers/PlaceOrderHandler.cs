using OrderingShipping.Orders.Domain;
using OrderingShipping.SharedKernel;

namespace OrderingShipping.Orders.Application;

public sealed class PlaceOrderHandler : ICommandHandler<PlaceOrder, OrderId>
{
    private readonly IOrderRepository _orders;
    private readonly DomainEventDispatcher _events;

    public PlaceOrderHandler(IOrderRepository orders, DomainEventDispatcher events)
    {
        _orders = orders;
        _events = events;
    }

    public OrderId Handle(PlaceOrder command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_orders.Find(command.OrderId) is not null)
        {
            throw new InvalidOperationException($"Order {command.OrderId} already exists.");
        }

        var order = Order.Create(
            command.OrderId,
            command.CustomerId,
            command.ShipTo,
            command.Lines);

        _orders.Add(order);
        _events.Dispatch(order.PullDomainEvents());
        return order.Id;
    }
}
