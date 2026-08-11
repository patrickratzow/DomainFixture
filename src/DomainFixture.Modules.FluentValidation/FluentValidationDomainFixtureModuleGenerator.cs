using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.Modules.FluentValidation;

[Generator]
public sealed class FluentValidationDomainFixtureModuleGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
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
