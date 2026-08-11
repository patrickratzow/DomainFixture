using System.Collections.Immutable;
using System.Linq;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Extraction;

internal static class DomainConstraintProviderCatalog
{
    public static DomainConstraintProviderSet Create(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<ConstraintExtractionResult>> moduleResults)
    {
        var manifestResults = ValidationRuleManifestProvider.Create(context);
        var allResults = moduleResults.Combine(manifestResults)
            .Select(static (results, _) => results.Left.AddRange(results.Right));

        return new DomainConstraintProviderSet(allResults);
    }
}
