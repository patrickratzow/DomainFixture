using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.Modules.FluentValidation;

internal static class ValidatorCandidateAnalyzer
{
    public static ValidatorCandidate? Analyze(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var declaration = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not
                INamedTypeSymbol validatorType ||
            FindValidatedType(validatorType) is not INamedTypeSymbol subjectType)
        {
            return null;
        }

        // Analyze a partial validator exactly once.
        if (validatorType.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree !=
            declaration.SyntaxTree)
            return null;

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        if (validatorType.IsAbstract || validatorType.IsGenericType ||
            !HasAccessibleParameterlessConstructor(validatorType))
        {
            diagnostics.Add(Diagnostic.Create(
                FluentValidationDiagnostics.InvalidValidator,
                declaration.Identifier.GetLocation(),
                validatorType.ToDisplayString(),
                "a non-abstract, non-generic validator with an accessible parameterless constructor is required"));
        }

        var rules = DiscoverRules(
            context.SemanticModel.Compilation,
            validatorType,
            subjectType,
            diagnostics,
            cancellationToken);
        var validatorTypeName = GeneratedTypeNames.Display(validatorType);
        var subjectTypeName = GeneratedTypeNames.Display(subjectType);
        var adapterClassName = GeneratedTypeNames.AdapterClassName(subjectType, validatorType);

        return new ValidatorCandidate(
            validatorTypeName,
            subjectTypeName,
            FluentValidationModuleConstants.GeneratedNamespace + "." + adapterClassName,
            adapterClassName,
            rules,
            diagnostics.ToImmutable(),
            declaration.Identifier.GetLocation());
    }

    private static ImmutableArray<RuleContribution> DiscoverRules(
        Compilation compilation,
        INamedTypeSymbol validatorType,
        INamedTypeSymbol subjectType,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        var rules = ImmutableArray.CreateBuilder<RuleContribution>();
        foreach (var syntaxReference in validatorType.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is not ClassDeclarationSyntax part)
                continue;

            var semanticModel = compilation.GetSemanticModel(part.SyntaxTree);
            foreach (var invocation in part.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var methodName = FluentValidationRuleParser.InvocationName(invocation);
                if (!FluentValidationRuleParser.IsRuleCandidate(methodName) ||
                    semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method ||
                    !method.ContainingNamespace.ToDisplayString()
                        .StartsWith("FluentValidation", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!FluentValidationRuleParser.IsSupported(methodName))
                {
                    diagnostics.Add(Diagnostic.Create(
                        FluentValidationDiagnostics.UnsupportedRule,
                        invocation.GetLocation(),
                        methodName));
                    continue;
                }

                var rule = FluentValidationRuleParser.TryCreate(
                    semanticModel,
                    invocation,
                    subjectType,
                    methodName,
                    diagnostics);
                if (rule is not null)
                    rules.Add(rule);
            }
        }

        return rules
            .GroupBy(rule => rule.Identity, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToImmutableArray();
    }

    private static ITypeSymbol? FindValidatedType(INamedTypeSymbol validatorType)
    {
        var validatorInterface = validatorType.AllInterfaces.FirstOrDefault(candidate =>
            candidate.OriginalDefinition.MetadataName == "IValidator`1" &&
            candidate.ContainingNamespace.ToDisplayString() == "FluentValidation");
        if (validatorInterface?.TypeArguments.Length == 1)
            return validatorInterface.TypeArguments[0];

        for (var current = validatorType.BaseType; current is not null; current = current.BaseType)
        {
            if (current.OriginalDefinition.MetadataName == "AbstractValidator`1" &&
                current.ContainingNamespace.ToDisplayString() == "FluentValidation" &&
                current.TypeArguments.Length == 1)
                return current.TypeArguments[0];
        }
        return null;
    }

    private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol validatorType) =>
        validatorType.InstanceConstructors.Any(constructor =>
            constructor.Parameters.Length == 0 &&
            constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal);
}
