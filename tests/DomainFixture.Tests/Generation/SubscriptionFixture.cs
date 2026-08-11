using DomainFixture.Generation;
using DomainFixture.Tests.Domain.Entities;

namespace DomainFixture.Tests.Generation;

public sealed class SubscriptionFixture : IFixtureTestConfiguration<Subscription>
{
    public void Configure(IFixtureTestBuilder<Subscription> fixture)
    {
        fixture.Recipe("Valid")
            .Synthesize()
            .State(
                "Initially pending",
                subject => subject.Details.Status,
                SubscriptionStatus.Pending)
            .Transition(
                "Activate",
                subject => subject.Activate(FixtureValue.Auto<int>()),
                subject => subject.Status,
                SubscriptionStatus.Active)
            .Transition(
                "Can reserve",
                subject => subject.CanReserve(FixtureValue.Auto<int>()),
                result => result);
    }
}
