using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.Modules.FluentValidation;

/// <summary>
/// Registers FluentValidation discovery and emission into a host source generator.
/// This type is a module component; it is not a Roslyn generator entry point.
/// </summary>
public sealed class FluentValidationModule
{
    public string Id => FluentValidationModuleConstants.ModuleId;

    public void Register(IncrementalGeneratorInitializationContext context)
    {
        var validators = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (syntaxContext, cancellationToken) =>
                    ValidatorCandidateAnalyzer.Analyze(syntaxContext, cancellationToken))
            .Where(static candidate => candidate is not null)
            .Select(static (candidate, _) => candidate!);

        context.RegisterSourceOutput(
            validators.Collect(),
            static (productionContext, candidates) =>
                FluentValidationModuleEmitter.Emit(productionContext, candidates));
    }
}
