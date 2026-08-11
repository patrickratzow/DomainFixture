using OrderingShipping.SharedKernel;

namespace OrderingShipping.Orders.Domain;

public sealed class Order : AggregateRoot<OrderId>
{
    private readonly IReadOnlyList<OrderLine> _lines;

    private Order(
        OrderId id,
        CustomerId customerId,
        ShippingAddress shipTo,
        IReadOnlyList<OrderLine> lines)
        : base(id)
    {
        CustomerId = customerId;
        ShipTo = shipTo;
        _lines = lines;
        Status = OrderStatus.Placed;
    }

    public CustomerId CustomerId { get; }
    public ShippingAddress ShipTo { get; }
    public IReadOnlyList<OrderLine> Lines => _lines;
    public OrderStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public Money Total => Money.Usd(_lines.Sum(line => line.LineTotal.Amount));

    public static Order Create(
        OrderId id,
        CustomerId customerId,
        ShippingAddress shipTo,
        IReadOnlyList<OrderLine> lines)
    {
        ArgumentNullException.ThrowIfNull(shipTo);
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
        {
            throw new ArgumentException("An order must contain at least one line.", nameof(lines));
        }

        var order = new Order(id, customerId, shipTo, lines.ToArray());
        order.Raise(new OrderPlaced(
            id,
            customerId,
            new ShippingAddressSnapshot(
                shipTo.Line1,
                shipTo.City,
                shipTo.PostalCode,
                shipTo.CountryCode),
            lines.Select(line => new OrderLineSnapshot(
                line.Sku.Value,
                line.Quantity,
                line.UnitPrice.Amount,
                line.UnitPrice.Currency)).ToArray(),
            order.Total.Amount,
            order.Total.Currency));

        return order;
    }

    public void Cancel(string reason)
    {
        if (Status != OrderStatus.Placed)
        {
            throw new InvalidOperationException("Only a placed order can be cancelled.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A cancellation reason is required.", nameof(reason));
        }

        Status = OrderStatus.Cancelled;
        CancellationReason = reason.Trim();
        Raise(new OrderCancelled(Id, CancellationReason));
    }
}
