using OrderingShipping.Orders.Domain;

namespace OrderingShipping.Orders.Application;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<OrderId, Order> _orders = new();

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _orders.Add(order.Id, order);
    }

    public Order? Find(OrderId orderId) =>
        _orders.TryGetValue(orderId, out var order) ? order : null;

    public void Save(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _orders[order.Id] = order;
    }
}
