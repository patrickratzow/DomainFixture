using FluentAssertions;
using NUnit.Framework;
using OrderingShipping.ExampleTests.Generation;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests;

[TestFixture]
public sealed class ShippingDomainTests
{
    [Test]
    public void DispatchAndDelivery_ShouldEmitEventsInLifecycleOrder()
    {
        var shipment = ShipmentFixtureFactory.Delivered.Create();

        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.DomainEvents.Select(domainEvent => domainEvent.GetType()).Should().Equal(
            typeof(ShipmentCreated),
            typeof(ShipmentDispatched),
            typeof(ShipmentDelivered));
    }

    [Test]
    public void DeliveringBeforeDispatch_ShouldBeRejected()
    {
        var shipment = ShipmentFixtureFactory.Pending.Create();

        var deliver = () => shipment.Deliver();

        deliver.Should().Throw<InvalidOperationException>();
        shipment.Status.Should().Be(ShipmentStatus.Pending);
    }
}
