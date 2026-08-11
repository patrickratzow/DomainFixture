using FluentAssertions;
using NUnit.Framework;
using OrderingShipping.ExampleTests.Generation;
using OrderingShipping.Orders.Domain;

namespace OrderingShipping.ExampleTests;

[TestFixture]
public sealed class OrderDomainTests
{
    [Test]
    public void PlacingAnOrder_ShouldCaptureAnImmutableIntegrationEvent()
    {
        var order = OrderFixtureFactory.Placed.Create();

        order.Status.Should().Be(OrderStatus.Placed);
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlaced>()
            .Which.Should().Match<OrderPlaced>(placed =>
                placed.OrderId == order.Id &&
                placed.Total == order.Total.Amount &&
                placed.Currency == order.Total.Currency &&
                placed.Lines.Count == order.Lines.Count);
    }

    [Test]
    public void CancellingTwice_ShouldProtectTheAggregateLifecycle()
    {
        var order = OrderFixtureFactory.Cancelled.Create();

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.OfType<OrderCancelled>().Should().ContainSingle();
        var secondCancellation = () => order.Cancel("again");
        secondCancellation.Should().Throw<InvalidOperationException>();
    }
}
