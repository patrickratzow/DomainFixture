using DomainFixture.Generation;
using DomainFixture.Tests.Domain.ValueObjects;

namespace DomainFixture.Tests.Generation;

public sealed class UsernameFixture : IFixtureTestConfiguration<Username>
{
    public void Configure(IFixtureTestBuilder<Username> fixture)
    {
        fixture.Recipe("Validation")
            .Baseline(Baseline);
    }

    public static Username Baseline() => Username.From("baseline");
}
