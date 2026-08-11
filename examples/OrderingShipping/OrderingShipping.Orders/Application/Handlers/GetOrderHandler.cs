using OrderingShipping.SharedKernel;

namespace OrderingShipping.Orders.Application;

public sealed class GetOrderHandler : IQueryHandler<GetOrder, OrderDetails?>
{
    private readonly IOrderRepository _orders;

    public GetOrderHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public OrderDetails? Handle(GetOrder query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var order = _orders.Find(query.OrderId);

        return order is null
            ? null
            : new OrderDetails(
                order.Id,
                order.CustomerId,
                order.Status,
                order.Total.Amount,
                order.Total.Currency,
                order.Lines.Count);
    }
}
