using FluentAssertions;
using NUnit.Framework;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests;

[TestFixture]
public sealed class ShippingDomainTests
{
    [Test]
    public void DispatchAndDelivery_ShouldEmitEventsInLifecycleOrder()
    {
        var shipment = CreateShipment();
        shipment.PullDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ShipmentCreated>();

        shipment.Dispatch(TrackingNumber.From("TRACK-900"));
        shipment.Status.Should().Be(ShipmentStatus.Dispatched);
        shipment.PullDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ShipmentDispatched>();

        shipment.Deliver();
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.PullDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ShipmentDelivered>();
    }

    [Test]
    public void DeliveringBeforeDispatch_ShouldBeRejected()
    {
        var shipment = CreateShipment();

        var deliver = () => shipment.Deliver();

        deliver.Should().Throw<InvalidOperationException>();
        shipment.Status.Should().Be(ShipmentStatus.Pending);
    }

    private static Shipment CreateShipment() =>
        Shipment.Create(
            ShipmentId.From(new Guid("30000000-0000-0000-0000-000000000303")),
            new Guid("10000000-0000-0000-0000-000000000101"),
            DeliveryAddress.Create("12 Domain Lane", "Copenhagen", "2100", "DK"));
}
