using DomainFixture.Generation;
using OrderingShipping.Orders.Application;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class PlaceOrderFixture : IFixtureTestConfiguration<PlaceOrder>
{
    public void Configure(IFixtureTestBuilder<PlaceOrder> fixture) =>
        fixture.Recipe("Valid");
}
