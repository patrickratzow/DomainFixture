using DomainFixture.Generation;
using OrderingShipping.Orders.Domain;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class OrderLineFixture : IFixtureTestConfiguration<OrderLine>
{
    public void Configure(IFixtureTestBuilder<OrderLine> fixture)
    {
        fixture.Recipe("Valid");
    }
}
