using DomainFixture.Generation;
using OrderingShipping.Shipping.Application;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class DeliverShipmentFixture : IFixtureTestConfiguration<DeliverShipment>
{
    public void Configure(IFixtureTestBuilder<DeliverShipment> fixture) =>
        fixture.Recipe("Valid");
}
