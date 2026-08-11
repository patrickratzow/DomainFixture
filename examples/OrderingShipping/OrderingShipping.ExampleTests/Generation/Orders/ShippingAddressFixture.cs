using DomainFixture.Generation;
using OrderingShipping.Orders.Domain;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class ShippingAddressFixture : IFixtureTestConfiguration<ShippingAddress>
{
    public void Configure(IFixtureTestBuilder<ShippingAddress> fixture)
    {
        fixture.Recipe("Valid")
            .Synthesize();
    }
}
