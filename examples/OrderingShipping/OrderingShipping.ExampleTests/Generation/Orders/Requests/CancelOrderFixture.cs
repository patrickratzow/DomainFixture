using DomainFixture.Generation;
using OrderingShipping.Orders.Application;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class CancelOrderFixture : IFixtureTestConfiguration<CancelOrder>
{
    public void Configure(IFixtureTestBuilder<CancelOrder> fixture) =>
        fixture.Recipe("Valid");
}
