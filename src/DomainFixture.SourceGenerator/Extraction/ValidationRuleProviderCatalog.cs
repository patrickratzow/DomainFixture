using System.Collections.Immutable;
using System.Linq;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Extraction;

internal static class DomainConstraintProviderCatalog
{
    public static DomainConstraintProviderSet Create(IncrementalGeneratorInitializationContext context)
    {
        // Framework-specific integrations are composed here. The generator entry point and
        // emitters consume shared DomainConstraintContract values plus source locations.
        var fluentValidationResults = FluentValidationConstraintAdapter.Create(context);
        var sourceResults = fluentValidationResults.Collect();
        var manifestResults = ValidationRuleManifestProvider.Create(context);
        var allResults = sourceResults.Combine(manifestResults)
            .Select(static (results, _) => results.Left.AddRange(results.Right));

        return new DomainConstraintProviderSet(sourceResults, allResults);
    }
}
