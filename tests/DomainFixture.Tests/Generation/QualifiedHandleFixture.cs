using DomainFixture.Generation;
using DomainFixture.Tests.Domain.ValueObjects;

namespace DomainFixture.Tests.Generation;

public sealed class QualifiedHandleFixture : IFixtureTestConfiguration<QualifiedHandle>
{
    public void Configure(IFixtureTestBuilder<QualifiedHandle> fixture)
    {
        fixture.Recipe("Validation")
            .Baseline(Baseline);
    }

    public static QualifiedHandle Baseline() => new("alice", "example", 5);
}
