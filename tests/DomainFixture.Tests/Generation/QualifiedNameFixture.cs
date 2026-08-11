using DomainFixture.Generation;
using DomainFixture.Tests.Domain.ValueObjects;

namespace DomainFixture.Tests.Generation;

public sealed class QualifiedNameFixture : IFixtureTestConfiguration<QualifiedName>
{
    public void Configure(IFixtureTestBuilder<QualifiedName> fixture)
    {
        fixture.Recipe("Validation")
            .Baseline(Baseline);
    }

    public static QualifiedName Baseline() => QualifiedName.From("alice", "example");
}
