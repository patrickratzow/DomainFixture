using FluentAssertions;
using NUnit.Framework;
using OrderingShipping.Orders.Domain;

namespace OrderingShipping.ExampleTests;

[TestFixture]
public sealed class OrderDomainTests
{
    [Test]
    public void PlacingAnOrder_ShouldCaptureAnImmutableIntegrationEvent()
    {
        var order = CreateOrder();

        order.Status.Should().Be(OrderStatus.Placed);
        order.Total.Should().Be(Money.Usd(50m));
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlaced>()
            .Which.Should().Match<OrderPlaced>(placed =>
                placed.OrderId == order.Id &&
                placed.Total == 50m &&
                placed.Currency == "USD" &&
                placed.Lines.Count == 1);
    }

    [Test]
    public void CancellingTwice_ShouldProtectTheAggregateLifecycle()
    {
        var order = CreateOrder();
        order.PullDomainEvents();

        order.Cancel("customer request");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCancelled>();
        var secondCancellation = () => order.Cancel("again");
        secondCancellation.Should().Throw<InvalidOperationException>();
    }

    private static Order CreateOrder() =>
        Order.Create(
            OrderId.From(new Guid("10000000-0000-0000-0000-000000000101")),
            CustomerId.From(new Guid("20000000-0000-0000-0000-000000000202")),
            ShippingAddress.Create("12 Domain Lane", "Copenhagen", "2100", "DK"),
            new[] { OrderLine.Create(Sku.From("DDD-BOOK"), 2, Money.Usd(25m)) });
}
