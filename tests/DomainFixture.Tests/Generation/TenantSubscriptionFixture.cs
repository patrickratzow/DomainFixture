using DomainFixture.Generation;
using DomainFixture.Tests.Domain.Entities;

namespace DomainFixture.Tests.Generation;

public sealed class TenantSubscriptionFixture : IFixtureTestConfiguration<TenantSubscription>
{
    public void Configure(IFixtureTestBuilder<TenantSubscription> fixture)
    {
        fixture.Recipe("Valid")
            .Synthesize();
    }
}
