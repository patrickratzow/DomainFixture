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
        first.Id.Should().NotBe(second.Id);
        first.CustomerId.Should().NotBe(second.CustomerId);
        first.Id.Value.Should().NotBe(first.CustomerId.Value);
        first.Lines[0].Sku.Should().NotBe(second.Lines[0].Sku);
        first.Status.Should().Be(OrderStatus.Placed);
        first.Lines.Should().ContainSingle();
        first.Lines[0].Quantity.Should().Be(1);
        first.Lines[0].Sku.Value.Should().NotBeNullOrWhiteSpace();
        first.Total.Should().Be(Money.Usd(1m));
        first.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlaced>();
    }

    [Test]
    public void GeneratedScenarioFactories_ShouldStayIsolated()
    {
        var pending = ShipmentFixtureFactory.Pending.Create();
        var dispatched = ShipmentFixtureFactory.Dispatched.Create();
        var delivered = ShipmentFixtureFactory.Delivered.Create();

        pending.Status.Should().Be(ShipmentStatus.Pending);
        dispatched.Status.Should().Be(ShipmentStatus.Dispatched);
        delivered.Status.Should().Be(ShipmentStatus.Delivered);

        dispatched.Deliver();

        dispatched.Status.Should().Be(ShipmentStatus.Delivered);
        ShipmentFixtureFactory.Dispatched.Create().Status.Should().Be(ShipmentStatus.Dispatched);
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
