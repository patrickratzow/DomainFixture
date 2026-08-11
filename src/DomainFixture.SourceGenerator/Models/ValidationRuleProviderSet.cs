using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class DomainConstraintProviderSet
{
    public IncrementalValueProvider<ImmutableArray<ConstraintExtractionResult>> AllResults { get; }

    public DomainConstraintProviderSet(
        IncrementalValueProvider<ImmutableArray<ConstraintExtractionResult>> allResults)
    {
        AllResults = allResults;
    }
}
