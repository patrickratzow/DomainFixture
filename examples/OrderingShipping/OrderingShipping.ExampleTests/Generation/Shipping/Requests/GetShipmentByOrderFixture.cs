using DomainFixture.Generation;
using OrderingShipping.Shipping.Application;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class GetShipmentByOrderFixture : IFixtureTestConfiguration<GetShipmentByOrder>
{
    public void Configure(IFixtureTestBuilder<GetShipmentByOrder> fixture) =>
        fixture.Recipe("Valid");
}
