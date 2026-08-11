using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        var useImmutableObjects = false;
        var useEntityIdentity = false;
        var useFluentValidation = false;
        var useFactories = false;
        string? serviceProviderFactoryType = null;
        var propertyMutations = ImmutableArray.CreateBuilder<PropertyMutationSpec>();
        var operationRejections = ImmutableArray.CreateBuilder<OperationRejectionSpec>();
        var operationResults = ImmutableArray.CreateBuilder<OperationResultSpec>();
        var configuredValues = ImmutableArray.CreateBuilder<ConfiguredValueSpec>();
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
                case "UseImmutableObjects":
                    useImmutableObjects = true;
                    break;
                case "UseEntityIdentity":
                    useEntityIdentity = true;
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
                case "RejectWith" when method.TypeArguments.Length == 1:
                    operationRejections.Add(new OperationRejectionSpec(
                        subjectTypeKey: null,
                        method.TypeArguments[0].ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat),
                        invocation.GetLocation()));
                    break;
                case "RejectWith" when method.TypeArguments.Length == 2:
                    operationRejections.Add(new OperationRejectionSpec(
                        method.TypeArguments[0].ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat),
                        method.TypeArguments[1].ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat),
                        invocation.GetLocation()));
                    break;
                case "UseResult" when method.TypeArguments.Length == 2:
                    var result = ParseOperationResult(
                        semanticModel,
                        invocation,
                        method);
                    if (result is null)
                    {
                        diagnostics.Add(GeneratorDiagnostics.GenerationProfileInvalid(
                            invocation.GetLocation(),
                            type.Name));
                    }
                    else
                    {
                        operationResults.Add(result);
                    }
                    break;
                case "For" when method.ContainingType.Name == "IFixtureValueOptions":
                    var configuredValue = ParseConfiguredValue(
                        semanticModel,
                        invocation,
                        method);
                    if (configuredValue is null)
                    {
                        var typeName = method.TypeArguments.FirstOrDefault()?.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat) ?? "unknown";
                        diagnostics.Add(GeneratorDiagnostics.ConfiguredValueInvalid(
                            invocation.GetLocation(),
                            typeName));
                    }
                    else
                    {
                        configuredValues.Add(configuredValue);
                    }
                    break;
                case "For" when method.ContainingType.Name == "IFixtureMutationOptions":
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

        foreach (var duplicate in operationRejections
                     .GroupBy(rejection => rejection.SubjectTypeKey)
                     .Where(group => group.Count() > 1))
        {
            var scope = duplicate.Key ?? "assembly";
            diagnostics.Add(GeneratorDiagnostics.OperationRejectionConflicting(
                duplicate.Skip(1).First().Location ?? declaration.GetLocation(),
                scope));
        }

        foreach (var duplicate in operationResults
                     .GroupBy(result => new { result.SubjectTypeName, result.ResultTypeName })
                     .Where(group => group.Count() > 1))
        {
            diagnostics.Add(GeneratorDiagnostics.OperationResultConflicting(
                duplicate.Skip(1).First().Location ?? declaration.GetLocation(),
                duplicate.Key.SubjectTypeName,
                duplicate.Key.ResultTypeName));
        }

        foreach (var duplicate in configuredValues
                     .GroupBy(value => value.TypeName)
                     .Where(group => group.Count() > 1))
        {
            diagnostics.Add(GeneratorDiagnostics.ConfiguredValueDuplicated(
                duplicate.Skip(1).First().Location ?? declaration.GetLocation(),
                duplicate.Key));
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
                useImmutableObjects,
                useEntityIdentity,
                useFluentValidation,
                serviceProviderFactoryType is null
                    ? FixtureActivationKind.Factories
                    : FixtureActivationKind.ServiceProvider,
                serviceProviderFactoryType,
                propertyMutations.ToImmutable(),
                operationRejections.ToImmutable(),
                operationResults.ToImmutable(),
                configuredValues.ToImmutable()),
            diagnostics.ToImmutable());
    }

    private static ConfiguredValueSpec? ParseConfiguredValue(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method)
    {
        if (method.TypeArguments.Length != 1 ||
            invocation.ArgumentList.Arguments.Count != 1)
            return null;

        var lambda = invocation.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        if (lambda?.Body is not ExpressionSyntax expression ||
            !ConfiguredValueExpressionFormatter.TryFormat(
                semanticModel,
                expression,
                out var formatted))
            return null;

        return new ConfiguredValueSpec(
            method.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            formatted!,
            invocation.GetLocation());
    }

    private static OperationResultSpec? ParseOperationResult(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method)
    {
        if (invocation.ArgumentList.Arguments.Count != 2)
            return null;
        var resultType = method.TypeArguments[1];
        if (!TryResolveMemberPath(
                semanticModel,
                invocation.ArgumentList.Arguments[0].Expression,
                resultType,
                out var successPath) ||
            !TryResolveMemberPath(
                semanticModel,
                invocation.ArgumentList.Arguments[1].Expression,
                resultType,
                out var valuePath))
            return null;

        return new OperationResultSpec(
            method.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            resultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            successPath!,
            valuePath!,
            invocation.GetLocation());
    }

    private static bool TryResolveMemberPath(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        ITypeSymbol parameterType,
        out string? memberPath)
    {
        memberPath = null;
        var lambda = expression as LambdaExpressionSyntax;
        ParameterSyntax? parameterSyntax = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 1 } parenthesized =>
                parenthesized.ParameterList.Parameters[0],
            _ => null
        };
        if (parameterSyntax is null ||
            semanticModel.GetDeclaredSymbol(parameterSyntax) is not IParameterSymbol parameter ||
            !SymbolEqualityComparer.Default.Equals(parameter.Type, parameterType) ||
            lambda!.Body is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var segments = new System.Collections.Generic.List<string>();
        ExpressionSyntax current = memberAccess;
        while (current is MemberAccessExpressionSyntax access)
        {
            var symbol = semanticModel.GetSymbolInfo(access).Symbol;
            var name = symbol switch
            {
                IPropertySymbol { IsStatic: false, Parameters.Length: 0 } property => property.Name,
                IFieldSymbol { IsStatic: false } field => field.Name,
                _ => null
            };
            if (name is null)
                return false;
            segments.Insert(0, EscapeIdentifier(name));
            current = access.Expression;
        }

        if (!SymbolEqualityComparer.Default.Equals(
                semanticModel.GetSymbolInfo(current).Symbol,
                parameter))
            return false;
        memberPath = string.Join(".", segments);
        return segments.Count > 0;
    }

    private static string EscapeIdentifier(string identifier) =>
        SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
        SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;

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
