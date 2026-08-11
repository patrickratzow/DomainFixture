using DomainFixture.Generation;
using DomainFixture.Tests.Domain.ValueObjects;

namespace DomainFixture.Tests.Generation;

public sealed class ResultDisplayNameFixture : IFixtureTestConfiguration<ResultDisplayName>
{
    public void Configure(IFixtureTestBuilder<ResultDisplayName> fixture)
    {
        fixture.Recipe("Valid")
            .Synthesize();
    }
}
