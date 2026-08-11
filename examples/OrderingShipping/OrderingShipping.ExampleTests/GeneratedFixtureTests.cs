using FluentAssertions;
using NUnit.Framework;
using OrderingShipping.ExampleTests.Generation;
using OrderingShipping.Orders.Domain;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests;

[TestFixture]
public sealed class GeneratedFixtureTests
{
    [Test]
    public void GeneratedOrderFactory_ShouldComposeANestedValidAggregate()
    {
        var first = OrderFixtureFactory.Placed.Create();
        var second = OrderFixtureFactory.Placed.Create();

        first.Should().NotBeSameAs(second);
        first.Status.Should().Be(OrderStatus.Placed);
        first.Lines.Should().ContainSingle();
        first.Lines[0].Quantity.Should().Be(2);
        first.Lines[0].Sku.Should().Be(Sku.From("DDD-BOOK"));
        first.Total.Should().Be(Money.Usd(50m));
        first.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlaced>();
    }

    [Test]
    public void GeneratedScenarioFactories_ShouldStayIsolated()
    {
        var pending = ShipmentFixtureFactory.Pending.Create();
        var delivered = ShipmentFixtureFactory.Delivered.Create();

        pending.Status.Should().Be(ShipmentStatus.Pending);
        delivered.Status.Should().Be(ShipmentStatus.Delivered);

        pending.Dispatch(TrackingNumber.From("TRACK-HANDWRITTEN"));

        pending.Status.Should().Be(ShipmentStatus.Dispatched);
        ShipmentFixtureFactory.Pending.Create().Status.Should().Be(ShipmentStatus.Pending);
    }

    [Test]
    public void GeneratedFactoryTransform_ShouldSupportHandwrittenScenarios()
    {
        var transformed = OrderFixtureFactory.Placed.Create(order =>
        {
            order.Cancel("test customization");
            return order;
        });

        transformed.Status.Should().Be(OrderStatus.Cancelled);
        OrderFixtureFactory.Placed.Create().Status.Should().Be(OrderStatus.Placed);
    }
}
