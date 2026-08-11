using DomainFixture.Generation;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class DeliveryAddressFixture : IFixtureTestConfiguration<DeliveryAddress>
{
    public void Configure(IFixtureTestBuilder<DeliveryAddress> fixture)
    {
        fixture.Recipe("Valid")
            .Synthesize();
    }
}
