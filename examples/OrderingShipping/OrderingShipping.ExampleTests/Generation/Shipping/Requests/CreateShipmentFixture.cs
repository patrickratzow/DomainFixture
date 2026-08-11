using DomainFixture.Generation;
using OrderingShipping.Shipping.Application;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class CreateShipmentFixture : IFixtureTestConfiguration<CreateShipment>
{
    public void Configure(IFixtureTestBuilder<CreateShipment> fixture) =>
        fixture.Recipe("Valid");
}
