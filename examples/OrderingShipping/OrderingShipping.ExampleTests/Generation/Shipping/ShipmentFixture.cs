using DomainFixture.Generation;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class ShipmentFixture : IFixtureTestConfiguration<Shipment>
{
    public void Configure(IFixtureTestBuilder<Shipment> fixture)
    {
        fixture.Recipe("Pending")
            .State("Starts pending", subject => subject.Status, ShipmentStatus.Pending)
            .Transition(
                "Dispatch",
                subject => subject.Dispatch(FixtureValue.Auto<TrackingNumber>()),
                subject => subject.Status,
                ShipmentStatus.Dispatched);

        fixture.Recipe("Dispatched")
            .FromTransition("Pending", "Dispatch")
            .Transition(
                "Deliver",
                subject => subject.Deliver(),
                subject => subject.Status,
                ShipmentStatus.Delivered);

        fixture.Recipe("Delivered")
            .FromTransition("Dispatched", "Deliver")
            .RejectTransition<InvalidOperationException>(
                "Cannot deliver twice",
                subject => subject.Deliver());
    }
}
