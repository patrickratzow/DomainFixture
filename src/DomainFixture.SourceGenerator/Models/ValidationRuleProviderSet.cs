using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class ValidationRuleProviderSet
{
    public IncrementalValueProvider<ImmutableArray<RuleExtractionResult>> SourceResults { get; }
    public IncrementalValueProvider<ImmutableArray<RuleExtractionResult>> AllResults { get; }

    public ValidationRuleProviderSet(
        IncrementalValueProvider<ImmutableArray<RuleExtractionResult>> sourceResults,
        IncrementalValueProvider<ImmutableArray<RuleExtractionResult>> allResults)
    {
        SourceResults = sourceResults;
        AllResults = allResults;
    }
}
