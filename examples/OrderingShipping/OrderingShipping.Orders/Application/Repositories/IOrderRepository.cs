using OrderingShipping.Orders.Domain;

namespace OrderingShipping.Orders.Application;

public interface IOrderRepository
{
    void Add(Order order);
    Order? Find(OrderId orderId);
    void Save(Order order);
}
