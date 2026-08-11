using DomainFixture.Generation;
using OrderingShipping.Orders.Application;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class GetOrderFixture : IFixtureTestConfiguration<GetOrder>
{
    public void Configure(IFixtureTestBuilder<GetOrder> fixture) =>
        fixture.Recipe("Valid");
}
