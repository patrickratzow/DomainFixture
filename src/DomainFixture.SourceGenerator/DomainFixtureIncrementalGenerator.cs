using DomainFixture.SourceGenerator.Discovery;
using DomainFixture.SourceGenerator.Emission;
using DomainFixture.SourceGenerator.Extraction;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator;

[Generator]
public sealed class DomainFixtureIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configurations = FluentFixtureConfigurationProvider.Create(context);
        var ruleResults = FluentValidationRuleProvider.Create(context);

        context.RegisterSourceOutput(configurations, static (productionContext, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
                productionContext.ReportDiagnostic(diagnostic);
        });

        context.RegisterSourceOutput(ruleResults, static (productionContext, result) =>
        {
            if (result.Diagnostic is not null)
                productionContext.ReportDiagnostic(result.Diagnostic);
        });

        var rules = ruleResults
            .Where(static result => result.Rule is not null)
            .Select(static (result, _) => result.Rule!);
        var generationInputs = configurations.Combine(rules.Collect());

        context.RegisterSourceOutput(generationInputs, static (productionContext, input) =>
            FixtureTestSourceEmitter.Emit(productionContext, input.Left, input.Right));
    }
}
