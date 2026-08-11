using DomainFixture.Generation;
using DomainFixture.Tests.Domain.Entities;

namespace DomainFixture.Tests.Generation;

public sealed class RegistrationRequestFixture : IFixtureTestConfiguration<RegistrationRequest>
{
    public void Configure(IFixtureTestBuilder<RegistrationRequest> fixture)
    {
        fixture.Recipe("Registration")
            .Baseline(Baseline);
    }

    public static RegistrationRequest Baseline() => new()
    {
        Description = "baseline"
    };
}
