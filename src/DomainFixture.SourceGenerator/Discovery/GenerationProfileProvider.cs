using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.SourceGenerator.Discovery;

internal static class GenerationProfileProvider
{
    private const string GenerationNamespace = "DomainFixture.Generation";
    private const string ProfileInterfaceName = "IFixtureGenerationProfile";

    public static IncrementalValueProvider<GenerationProfileParseResult> Create(
        IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (syntaxContext, cancellationToken) =>
                    ParseCandidate(syntaxContext, cancellationToken))
            .Where(static candidate => candidate is not null)
            .Select(static (candidate, _) => candidate!);

        return candidates.Collect().Select(static (items, _) => Merge(items));
    }

    private static ProfileCandidate? ParseCandidate(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var declaration = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type ||
            !type.AllInterfaces.Any(candidate =>
                candidate.Name == ProfileInterfaceName &&
                candidate.ContainingNamespace.ToDisplayString() == GenerationNamespace))
        {
            return null;
        }

        var configureMethod = type.GetMembers("Configure")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method => method.Parameters.Length == 1);
        if (configureMethod?.DeclaringSyntaxReferences.FirstOrDefault()
                ?.GetSyntax(cancellationToken) is not MethodDeclarationSyntax configureDeclaration)
        {
            return ProfileCandidate.Invalid(
                type.Name,
                type.Locations.FirstOrDefault(),
                GeneratorDiagnostics.GenerationProfileInvalid(type.Locations.FirstOrDefault(), type.Name));
        }

        var semanticModel = context.SemanticModel.Compilation.GetSemanticModel(
            configureDeclaration.SyntaxTree);
        var useNullability = false;
        var usePropertyNames = false;
        var useFluentValidation = false;
        var useFactories = false;
        string? serviceProviderFactoryType = null;
        var propertyMutations = ImmutableArray.CreateBuilder<PropertyMutationSpec>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        foreach (var invocation in configureDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method ||
                method.ContainingNamespace.ToDisplayString() != GenerationNamespace)
            {
                continue;
            }

            switch (method.Name)
            {
                case "UseNullability":
                    useNullability = true;
                    break;
                case "UsePropertyNames":
                    usePropertyNames = true;
                    break;
                case "UseFluentValidation":
                    useFluentValidation = true;
                    break;
                case "UseFactories":
                    useFactories = true;
                    break;
                case "UseServiceProvider" when method.TypeArguments.FirstOrDefault() is ITypeSymbol factoryType:
                    serviceProviderFactoryType = factoryType.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat);
                    break;
                case "For":
                    var mutation = ParsePropertyMutation(
                        semanticModel,
                        invocation,
                        method,
                        cancellationToken);
                    if (mutation is null)
                    {
                        diagnostics.Add(GeneratorDiagnostics.PropertyMutationInvalid(
                            invocation.GetLocation(),
                            type.Name));
                    }
                    else
                    {
                        propertyMutations.Add(mutation);
                    }
                    break;
            }
        }

        foreach (var duplicate in propertyMutations
                     .GroupBy(mutation => new { mutation.SubjectTypeKey, mutation.PropertyName })
                     .Where(group => group.Count() > 1))
        {
            diagnostics.Add(GeneratorDiagnostics.PropertyMutationDuplicated(
                declaration.GetLocation(),
                type.Name,
                duplicate.Key.SubjectTypeKey,
                duplicate.Key.PropertyName));
        }

        if (useFactories && serviceProviderFactoryType is not null)
        {
            return ProfileCandidate.Invalid(
                type.Name,
                declaration.GetLocation(),
                GeneratorDiagnostics.GenerationActivationConflicting(
                    declaration.GetLocation(),
                    type.Name));
        }

        return new ProfileCandidate(
            type.Name,
            declaration.GetLocation(),
            new GenerationProfileSpec(
                useNullability,
                usePropertyNames,
                useFluentValidation,
                serviceProviderFactoryType is null
                    ? FixtureActivationKind.Factories
                    : FixtureActivationKind.ServiceProvider,
                serviceProviderFactoryType,
                propertyMutations.ToImmutable()),
            diagnostics.ToImmutable());
    }

    private static PropertyMutationSpec? ParsePropertyMutation(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        CancellationToken cancellationToken)
    {
        if (method.TypeArguments.Length != 2 || invocation.ArgumentList.Arguments.Count != 2)
            return null;

        var propertyExpression = invocation.ArgumentList.Arguments[0].Expression switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Body as ExpressionSyntax,
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.Body as ExpressionSyntax,
            _ => null
        };
        if (propertyExpression is null ||
            semanticModel.GetSymbolInfo(propertyExpression, cancellationToken).Symbol is not IPropertySymbol property)
        {
            return null;
        }

        var reconstructionExpression = invocation.ArgumentList.Arguments[1].Expression;
        var symbolInfo = semanticModel.GetSymbolInfo(reconstructionExpression, cancellationToken);
        var reconstruction = symbolInfo.Symbol as IMethodSymbol ??
            symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
        if (reconstruction is null ||
            !reconstruction.IsStatic ||
            reconstruction.DeclaredAccessibility is not (Accessibility.Public or
                Accessibility.Internal or
                Accessibility.ProtectedOrInternal))
        {
            return null;
        }

        return new PropertyMutationSpec(
            method.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            property.Name,
            $"{reconstruction.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{reconstruction.Name}");
    }

    private static GenerationProfileParseResult Merge(ImmutableArray<ProfileCandidate> candidates)
    {
        if (candidates.IsEmpty)
        {
            return new GenerationProfileParseResult(
                GenerationProfileSpec.Default,
                ImmutableArray<Diagnostic>.Empty);
        }

        var diagnostics = candidates.SelectMany(candidate => candidate.Diagnostics).ToImmutableArray();
        if (candidates.Length > 1)
        {
            diagnostics = diagnostics.Add(
                GeneratorDiagnostics.GenerationProfileDuplicated(candidates[1].Location));
        }

        return new GenerationProfileParseResult(candidates[0].Profile, diagnostics);
    }

    private sealed class ProfileCandidate
    {
        public string Name { get; }
        public Location? Location { get; }
        public GenerationProfileSpec Profile { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public ProfileCandidate(
            string name,
            Location? location,
            GenerationProfileSpec profile,
            ImmutableArray<Diagnostic> diagnostics)
        {
            Name = name;
            Location = location;
            Profile = profile;
            Diagnostics = diagnostics;
        }

        public static ProfileCandidate Invalid(
            string name,
            Location? location,
            Diagnostic diagnostic) =>
            new(name, location, GenerationProfileSpec.Default, ImmutableArray.Create(diagnostic));
    }
}
