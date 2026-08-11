using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.SourceGenerator.Discovery;

internal static class FluentFixtureConfigurationProvider
{
    private const string GenerationNamespace = "DomainFixture.Generation";
    private const string ConfigurationInterfaceMetadataName = "IFixtureTestConfiguration`1";
    private const string ValidatorInterfaceMetadataName = "IFixtureValidator`1";
    private const string ValidatorNamespace = "DomainFixture.Validation";

    public static IncrementalValuesProvider<ConfigurationParseResult> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (syntaxContext, cancellationToken) =>
                    ParseCandidate(syntaxContext, cancellationToken))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);
    }

    private static ConfigurationParseResult? ParseCandidate(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var declaration = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type)
            return null;

        var configurationInterface = type.AllInterfaces.FirstOrDefault(candidate =>
            candidate.OriginalDefinition.MetadataName == ConfigurationInterfaceMetadataName &&
            candidate.OriginalDefinition.ContainingNamespace.ToDisplayString() == GenerationNamespace);
        if (configurationInterface is null || configurationInterface.TypeArguments.Length != 1)
            return null;

        var subjectType = configurationInterface.TypeArguments[0];
        var configureMethod = type.GetMembers("Configure")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method => method.Parameters.Length == 1);
        var syntaxReference = configureMethod?.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference?.GetSyntax(cancellationToken) is not MethodDeclarationSyntax configureDeclaration)
        {
            return Failure(GeneratorDiagnostics.IncompleteConfiguration(
                type.Locations.FirstOrDefault(),
                type.Name));
        }

        var semanticModel = context.SemanticModel.Compilation.GetSemanticModel(
            configureDeclaration.SyntaxTree);
        var configurations = ImmutableArray.CreateBuilder<FixtureGenerationSpec>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var recipeChains = configureDeclaration.DescendantNodes()
            .OfType<ExpressionStatementSyntax>()
            .Select(statement => statement.Expression as InvocationExpressionSyntax)
            .Where(invocation => invocation is not null &&
                EnumerateChain(invocation).Any(candidate =>
                    IsGenerationMethod(semanticModel, candidate, "Recipe")))
            .Select(invocation => invocation!)
            .ToArray();
        if (recipeChains.Length == 0)
        {
            diagnostics.Add(GeneratorDiagnostics.IncompleteConfiguration(
                configureDeclaration.GetLocation(),
                type.Name));
        }

        foreach (var outerInvocation in recipeChains)
        {
            var parsed = ParseRecipeChain(
                semanticModel,
                type,
                subjectType,
                outerInvocation,
                diagnostics);
            if (parsed is not null)
                configurations.Add(parsed);
        }

        foreach (var duplicate in configurations
                     .GroupBy(configuration => configuration.RecipeName)
                     .Where(group => group.Count() > 1))
        {
            diagnostics.Add(GeneratorDiagnostics.RecipeDuplicated(
                type.Locations.FirstOrDefault(),
                type.Name,
                duplicate.Key));
        }

        return new ConfigurationParseResult(configurations.ToImmutable(), diagnostics.ToImmutable());
    }

    private static FixtureGenerationSpec? ParseRecipeChain(
        SemanticModel semanticModel,
        INamedTypeSymbol configurationType,
        ITypeSymbol subjectType,
        InvocationExpressionSyntax outerInvocation,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        string? recipeName = null;
        IMethodSymbol? baselineFactory = null;
        IMethodSymbol? validatorFactory = null;
        INamedTypeSymbol? validationRulesType = null;

        foreach (var invocation in EnumerateChain(outerInvocation))
        {
            if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
                method.ContainingNamespace.ToDisplayString() != GenerationNamespace)
            {
                continue;
            }

            switch (method.Name)
            {
                case "Recipe" when invocation.ArgumentList.Arguments.Count == 1:
                    var recipeValue = semanticModel.GetConstantValue(
                        invocation.ArgumentList.Arguments[0].Expression);
                    recipeName = recipeValue.HasValue ? recipeValue.Value as string : null;
                    break;

                case "Baseline" when invocation.ArgumentList.Arguments.Count == 1:
                    baselineFactory = ResolveMethodGroup(
                        semanticModel,
                        invocation.ArgumentList.Arguments[0].Expression);
                    break;

                case "ValidateWith" when invocation.ArgumentList.Arguments.Count == 1:
                    validatorFactory = ResolveMethodGroup(
                        semanticModel,
                        invocation.ArgumentList.Arguments[0].Expression);
                    break;

                case "RulesFrom" when method.TypeArguments.FirstOrDefault() is INamedTypeSymbol rulesType:
                    validationRulesType = rulesType;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(recipeName) ||
            baselineFactory is null)
        {
            diagnostics.Add(GeneratorDiagnostics.IncompleteConfiguration(
                outerInvocation.GetLocation(),
                configurationType.Name));
            return null;
        }

        if (!IsCallableFactory(baselineFactory))
        {
            diagnostics.Add(GeneratorDiagnostics.InaccessibleFactory(
                baselineFactory.Locations.FirstOrDefault() ?? outerInvocation.GetLocation(),
                baselineFactory.ToDisplayString()));
            return null;
        }

        if (validatorFactory is not null && !IsCallableFactory(validatorFactory))
        {
            diagnostics.Add(GeneratorDiagnostics.InaccessibleFactory(
                validatorFactory.Locations.FirstOrDefault() ?? outerInvocation.GetLocation(),
                validatorFactory.ToDisplayString()));
            return null;
        }

        if (!SymbolEqualityComparer.Default.Equals(baselineFactory.ReturnType, subjectType))
        {
            diagnostics.Add(GeneratorDiagnostics.IncompleteConfiguration(
                outerInvocation.GetLocation(),
                configurationType.Name));
            return null;
        }

        if (validatorFactory is not null &&
            !ImplementsFixtureValidator(validatorFactory.ReturnType, subjectType))
        {
            diagnostics.Add(GeneratorDiagnostics.IncompatibleValidator(
                validatorFactory.Locations.FirstOrDefault() ?? outerInvocation.GetLocation(),
                configurationType.Name,
                subjectType.ToDisplayString()));
            return null;
        }

        var namespaceName = configurationType.ContainingNamespace.IsGlobalNamespace
            ? "DomainFixture.Generated"
            : configurationType.ContainingNamespace.ToDisplayString();

        return new FixtureGenerationSpec(
            configurationType.Name,
            namespaceName,
            recipeName!,
            subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            subjectType.Name,
            CreateFactoryExpression(baselineFactory),
            validatorFactory is null ? null : CreateFactoryExpression(validatorFactory),
            validationRulesType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            DiscoverProperties(subjectType, semanticModel.Compilation.Assembly),
            CanUseDerivedReconstruction(subjectType, semanticModel.Compilation.Assembly),
            outerInvocation.GetLocation());
    }

    private static ImmutableArray<SubjectPropertySpec> DiscoverProperties(
        ITypeSymbol subjectType,
        IAssemblySymbol currentAssembly)
    {
        var properties = ImmutableArray.CreateBuilder<SubjectPropertySpec>();
        var seenNames = new HashSet<string>();

        for (var current = subjectType as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!seenNames.Add(property.Name))
                    continue;

                properties.Add(new SubjectPropertySpec(
                    property.Name,
                    property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    property.Type.SpecialType == SpecialType.System_String,
                    property.NullableAnnotation == NullableAnnotation.NotAnnotated,
                    IsAccessibleFromGeneratedCode(property.SetMethod, currentAssembly),
                    property.SetMethod is not null,
                    IsSetterAccessibleFromDerivedType(property.SetMethod, currentAssembly),
                    IsAccessibleFromGeneratedCode(property.GetMethod, currentAssembly)));
            }
        }

        return properties.ToImmutable();
    }

    private static bool CanUseDerivedReconstruction(
        ITypeSymbol subjectType,
        IAssemblySymbol currentAssembly)
    {
        if (subjectType is not INamedTypeSymbol namedType ||
            namedType.TypeKind != TypeKind.Class ||
            namedType.IsSealed ||
            !namedType.InstanceConstructors.Any(constructor =>
                constructor.Parameters.Length == 0 &&
                IsConstructorAccessibleFromDerivedType(constructor, currentAssembly)))
        {
            return false;
        }

        var properties = DiscoverProperties(subjectType, currentAssembly);
        return properties
            .Where(property => property.HasSetter)
            .All(property =>
                property.CanSetFromDerivedType &&
                property.CanReadFromGeneratedCode);
    }

    private static bool IsAccessibleFromGeneratedCode(
        IMethodSymbol? method,
        IAssemblySymbol currentAssembly)
    {
        if (method is null)
            return false;

        var sameAssembly = SymbolEqualityComparer.Default.Equals(
            method.ContainingAssembly,
            currentAssembly);
        return method.DeclaredAccessibility == Accessibility.Public ||
               sameAssembly && method.DeclaredAccessibility is Accessibility.Internal or
                   Accessibility.ProtectedOrInternal;
    }

    private static bool IsSetterAccessibleFromDerivedType(
        IMethodSymbol? method,
        IAssemblySymbol currentAssembly)
    {
        if (method is null)
            return false;

        var sameAssembly = SymbolEqualityComparer.Default.Equals(
            method.ContainingAssembly,
            currentAssembly);
        return method.DeclaredAccessibility is Accessibility.Public or
                   Accessibility.Protected or
                   Accessibility.ProtectedOrInternal ||
               sameAssembly && method.DeclaredAccessibility is Accessibility.Internal or
                   Accessibility.ProtectedAndInternal;
    }

    private static bool IsConstructorAccessibleFromDerivedType(
        IMethodSymbol constructor,
        IAssemblySymbol currentAssembly)
    {
        var sameAssembly = SymbolEqualityComparer.Default.Equals(
            constructor.ContainingAssembly,
            currentAssembly);
        return constructor.DeclaredAccessibility is Accessibility.Public or
                   Accessibility.Protected or
                   Accessibility.ProtectedOrInternal ||
               sameAssembly && constructor.DeclaredAccessibility is Accessibility.Internal or
                   Accessibility.ProtectedAndInternal;
    }

    private static IEnumerable<InvocationExpressionSyntax> EnumerateChain(
        InvocationExpressionSyntax outerInvocation)
    {
        var current = outerInvocation;
        while (true)
        {
            yield return current;

            if (current.Expression is not MemberAccessExpressionSyntax memberAccess ||
                memberAccess.Expression is not InvocationExpressionSyntax previous)
            {
                yield break;
            }

            current = previous;
        }
    }

    private static bool IsGenerationMethod(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        string methodName)
    {
        return semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
               method.Name == methodName &&
               method.ContainingNamespace.ToDisplayString() == GenerationNamespace;
    }

    private static IMethodSymbol? ResolveMethodGroup(
        SemanticModel semanticModel,
        ExpressionSyntax expression)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(expression);
        return symbolInfo.Symbol as IMethodSymbol ??
               symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
    }

    private static bool IsCallableFactory(IMethodSymbol method)
    {
        return method.IsStatic &&
               method.Parameters.Length == 0 &&
               method.DeclaredAccessibility is Accessibility.Public or
                   Accessibility.Internal or
                   Accessibility.ProtectedOrInternal;
    }

    private static bool ImplementsFixtureValidator(ITypeSymbol validatorType, ITypeSymbol subjectType)
    {
        var candidates = validatorType is INamedTypeSymbol named
            ? named.AllInterfaces.Add(named)
            : ImmutableArray<INamedTypeSymbol>.Empty;

        return candidates.Any(candidate =>
            candidate.OriginalDefinition.MetadataName == ValidatorInterfaceMetadataName &&
            candidate.OriginalDefinition.ContainingNamespace.ToDisplayString() == ValidatorNamespace &&
            candidate.TypeArguments.Length == 1 &&
            SymbolEqualityComparer.Default.Equals(candidate.TypeArguments[0], subjectType));
    }

    private static string CreateFactoryExpression(IMethodSymbol factory)
    {
        var containingType = factory.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"{containingType}.{factory.Name}()";
    }

    private static ConfigurationParseResult Failure(Diagnostic diagnostic)
    {
        return new ConfigurationParseResult(
            ImmutableArray<FixtureGenerationSpec>.Empty,
            ImmutableArray.Create(diagnostic));
    }
}
