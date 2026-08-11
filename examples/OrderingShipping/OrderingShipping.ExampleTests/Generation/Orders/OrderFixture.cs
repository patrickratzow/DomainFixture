using DomainFixture.Generation;
using OrderingShipping.Orders.Domain;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class OrderFixture : IFixtureTestConfiguration<Order>
{
    public void Configure(IFixtureTestBuilder<Order> fixture)
    {
        fixture.Recipe("Placed")
            .State("Starts placed", subject => subject.Status, OrderStatus.Placed)
            .Transition(
                "Cancel",
                subject => subject.Cancel("customer request"),
                subject => subject.Status,
                OrderStatus.Cancelled);

        fixture.Recipe("Cancelled")
            .FromTransition("Placed", "Cancel")
            .RejectTransition<InvalidOperationException>(
                "Cannot cancel twice",
                subject => subject.Cancel("second request"));
    }
}
