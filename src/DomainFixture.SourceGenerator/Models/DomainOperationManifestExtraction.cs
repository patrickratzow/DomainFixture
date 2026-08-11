using System.Collections.Immutable;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class DomainOperationManifestExtraction
{
    public ImmutableArray<DiscoveredDomainOperation> Operations { get; }
    public ImmutableArray<DiscoveredDomainOperationOutcome> Outcomes { get; }
    public ImmutableArray<DomainOperationManifestFailure> Failures { get; }

    public DomainOperationManifestExtraction(
        ImmutableArray<DiscoveredDomainOperation> operations,
        ImmutableArray<DiscoveredDomainOperationOutcome> outcomes,
        ImmutableArray<DomainOperationManifestFailure> failures)
    {
        Operations = operations;
        Outcomes = outcomes;
        Failures = failures;
    }
}
