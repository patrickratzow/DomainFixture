using DomainFixture.Generation;
using OrderingShipping.Shipping.Application;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class DispatchShipmentFixture : IFixtureTestConfiguration<DispatchShipment>
{
    public void Configure(IFixtureTestBuilder<DispatchShipment> fixture) =>
        fixture.Recipe("Valid");
}
