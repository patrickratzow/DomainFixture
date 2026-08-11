using DomainFixture.SourceGenerator.Discovery;
using DomainFixture.SourceGenerator.Emission;
using DomainFixture.SourceGenerator.Extraction;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator;

[Generator]
public sealed class DomainFixtureIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configurations = FluentFixtureConfigurationProvider.Create(context);
        var generationProfile = GenerationProfileProvider.Create(context);
        var ruleProviders = ValidationRuleProviderCatalog.Create(context);

        context.RegisterSourceOutput(configurations, static (productionContext, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
                productionContext.ReportDiagnostic(diagnostic);
        });

        context.RegisterSourceOutput(generationProfile, static (productionContext, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
                productionContext.ReportDiagnostic(diagnostic);
        });

        context.RegisterSourceOutput(ruleProviders.AllResults, static (productionContext, results) =>
        {
            foreach (var result in results)
            {
                if (result.Diagnostic is not null)
                    productionContext.ReportDiagnostic(result.Diagnostic);
            }
        });

        context.RegisterSourceOutput(
            ruleProviders.SourceResults,
            static (productionContext, results) =>
                ValidationRuleManifestEmitter.Emit(productionContext, results));

        var rules = ruleProviders.AllResults.Select(static (results, _) => results
            .Where(result => result.Rule is not null)
            .Select(result => result.Rule!)
            .ToImmutableArray());
        var generationInputs = configurations.Combine(generationProfile).Combine(rules);

        context.RegisterSourceOutput(generationInputs, static (productionContext, input) =>
            FixtureTestSourceEmitter.Emit(
                productionContext,
                input.Left.Left,
                input.Left.Right,
                input.Right));
    }
}
