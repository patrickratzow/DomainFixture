using DomainFixture.Generation;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class ShipmentFixture : IFixtureTestConfiguration<Shipment>
{
    private static readonly Guid SourceOrderId =
        new("10000000-0000-0000-0000-000000000099");

    public void Configure(IFixtureTestBuilder<Shipment> fixture)
    {
        fixture.Recipe("Pending")
            .Synthesize()
            .State("Starts pending", subject => subject.Status, ShipmentStatus.Pending)
            .Transition(
                "Dispatch",
                subject => subject.Dispatch(FixtureValue.Auto<TrackingNumber>()),
                subject => subject.Status,
                ShipmentStatus.Dispatched);

        fixture.Recipe("Dispatched")
            .Baseline(Dispatched)
            .Transition(
                "Deliver",
                subject => subject.Deliver(),
                subject => subject.Status,
                ShipmentStatus.Delivered);

        fixture.Recipe("Delivered")
            .Baseline(Delivered)
            .RejectTransition<InvalidOperationException>(
                "Cannot deliver twice",
                subject => subject.Deliver());
    }

    public static Shipment Dispatched()
    {
        var shipment = NewShipment(new Guid("30000000-0000-0000-0000-000000000030"));
        shipment.PullDomainEvents();
        shipment.Dispatch(TrackingNumber.From("TRACK-030"));
        return shipment;
    }

    public static Shipment Delivered()
    {
        var shipment = NewShipment(new Guid("30000000-0000-0000-0000-000000000031"));
        shipment.PullDomainEvents();
        shipment.Dispatch(TrackingNumber.From("TRACK-031"));
        shipment.PullDomainEvents();
        shipment.Deliver();
        return shipment;
    }

    private static Shipment NewShipment(Guid id) =>
        Shipment.Create(
            ShipmentId.From(id),
            SourceOrderId,
            DeliveryAddress.Create("12 Domain Lane", "Copenhagen", "2100", "DK"));
}
