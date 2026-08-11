using DomainFixture.Generation;
using OrderingShipping.Orders.Domain;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class OrderFixture : IFixtureTestConfiguration<Order>
{
    private static readonly OrderId CancelledOrderId =
        OrderId.From(new Guid("10000000-0000-0000-0000-000000000010"));

    public void Configure(IFixtureTestBuilder<Order> fixture)
    {
        fixture.Recipe("Placed")
            .Synthesize()
            .State("Starts placed", subject => subject.Status, OrderStatus.Placed)
            .Transition(
                "Cancel",
                subject => subject.Cancel("customer request"),
                subject => subject.Status,
                OrderStatus.Cancelled);

        fixture.Recipe("Cancelled")
            .Baseline(Cancelled)
            .RejectTransition<InvalidOperationException>(
                "Cannot cancel twice",
                subject => subject.Cancel("second request"));
    }

    public static Order Cancelled()
    {
        var order = Order.Create(
            CancelledOrderId,
            CustomerId.From(new Guid("20000000-0000-0000-0000-000000000020")),
            ShippingAddress.Create("12 Domain Lane", "Copenhagen", "2100", "DK"),
            new[] { OrderLine.Create(Sku.From("DDD-BOOK"), 1, Money.Usd(25m)) });

        order.PullDomainEvents();
        order.Cancel("customer request");
        return order;
    }
}
