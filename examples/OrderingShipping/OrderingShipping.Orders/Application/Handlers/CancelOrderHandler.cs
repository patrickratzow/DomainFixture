using OrderingShipping.Orders.Domain;
using OrderingShipping.SharedKernel;

namespace OrderingShipping.Orders.Application;

public sealed class CancelOrderHandler : ICommandHandler<CancelOrder, bool>
{
    private readonly IOrderRepository _orders;
    private readonly DomainEventDispatcher _events;

    public CancelOrderHandler(IOrderRepository orders, DomainEventDispatcher events)
    {
        _orders = orders;
        _events = events;
    }

    public bool Handle(CancelOrder command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var order = _orders.Find(command.OrderId)
            ?? throw new KeyNotFoundException($"Order {command.OrderId} was not found.");

        order.Cancel(command.Reason);
        _orders.Save(order);
        _events.Dispatch(order.PullDomainEvents());
        return true;
    }
}
