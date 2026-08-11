using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Models;
using DomainFixture.TestGenerator.Modules;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Discovery;

/// <summary>
/// Expands explicit fixture roots into a bounded graph of constructible domain values.
/// This deliberately does not perform assembly-wide type or CQRS request scanning.
/// </summary>
internal static class ImplicitFixtureConfigurationDiscovery
{
    public static ConfigurationParseResult DiscoverContributedRecipes(
        Compilation compilation,
        ImmutableArray<FixtureGenerationSpec> explicitConfigurations,
        ImmutableArray<CompileTimeRecipeContribution> contributions)
    {
        var configurations = ImmutableArray.CreateBuilder<FixtureGenerationSpec>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var identities = explicitConfigurations
            .GroupBy(ConfigurationIdentity, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var contribution in contributions)
        {
            var identity = contribution.ConfigurationNamespace + "|" +
                           contribution.ConfigurationName + "|" + contribution.RecipeName;
            if (identities.TryGetValue(identity, out var existing))
            {
                if (existing.SubjectTypeName != contribution.SubjectTypeName)
                {
                    diagnostics.Add(GeneratorDiagnostics.CompileTimeModuleContributionInvalid(
                        location: null,
                        contribution.ModuleId,
                        $"recipe '{identity}' conflicts with explicit subject '{existing.SubjectTypeName}'"));
                }
                // A handwritten recipe is authoritative over an equivalent module default.
                continue;
            }

            var subjectType = ResolveType(compilation, contribution.SubjectTypeName);
            if (subjectType is null)
            {
                diagnostics.Add(GeneratorDiagnostics.CompileTimeModuleContributionInvalid(
                    location: null,
                    contribution.ModuleId,
                    $"subject type '{contribution.SubjectTypeName}' is not available to the test compilation"));
                continue;
            }

            var configuration = CreateConfiguration(
                compilation,
                subjectType,
                contribution.ConfigurationNamespace,
                contribution.ConfigurationName,
                contribution.RecipeName,
                location: null,
                diagnostics);
            if (configuration is null)
            {
                diagnostics.Add(GeneratorDiagnostics.CompileTimeModuleContributionInvalid(
                    location: null,
                    contribution.ModuleId,
                    $"subject '{contribution.SubjectTypeName}' has no supported construction path"));
                continue;
            }

            configurations.Add(configuration);
            identities.Add(identity, configuration);
        }

        return new ConfigurationParseResult(
            configurations.ToImmutable(),
            diagnostics.ToImmutable());
    }

    public static ConfigurationParseResult Discover(
        Compilation compilation,
        ImmutableArray<FixtureGenerationSpec> explicitConfigurations,
        GenerationProfileSpec profile)
    {
        if (!profile.AutoDiscoverDomainTypes || explicitConfigurations.IsEmpty)
        {
            return new ConfigurationParseResult(
                ImmutableArray<FixtureGenerationSpec>.Empty,
                ImmutableArray<Diagnostic>.Empty);
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var configurations = ImmutableArray.CreateBuilder<FixtureGenerationSpec>();
        var explicitSubjects = new HashSet<string>(
            explicitConfigurations.Select(configuration => configuration.SubjectTypeName),
            StringComparer.Ordinal);
        var occupiedNames = new HashSet<string>(
            explicitConfigurations.Select(configuration =>
                configuration.NamespaceName + "|" + configuration.ConfigurationName),
            StringComparer.Ordinal);
        var visited = new HashSet<string>(explicitSubjects, StringComparer.Ordinal);
        var queued = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<DiscoveryEntry>();

        foreach (var root in explicitConfigurations)
        {
            var symbol = ResolveType(compilation, root.SubjectTypeName);
            if (symbol is null)
                continue;

            EnqueueDependencies(symbol, root.NamespaceName, root.Location);
        }

        while (queue.Count > 0)
        {
            var entry = queue.Dequeue();
            var typeName = Display(entry.Type);
            if (!visited.Add(typeName))
                continue;

            EnqueueDependencies(entry.Type, entry.NamespaceName, entry.Location);

            var configurationName = CreateConfigurationName(
                entry.Type,
                entry.NamespaceName,
                occupiedNames);
            var configuration = CreateConfiguration(
                compilation,
                entry.Type,
                entry.NamespaceName,
                configurationName,
                "Valid",
                entry.Location,
                diagnostics);
            // Unsupported nodes remain traversable, but do not create broken public factories.
            if (configuration is not null)
                configurations.Add(configuration);
        }

        return new ConfigurationParseResult(
            configurations.ToImmutable(),
            diagnostics.ToImmutable());

        void EnqueueDependencies(
            INamedTypeSymbol owner,
            string namespaceName,
            Location? location)
        {
            foreach (var property in EnumerateReadableProperties(owner, compilation.Assembly))
            {
                foreach (var dependency in ExpandDomainTypes(property.Type))
                {
                    var dependencyName = Display(dependency);
                    if (!visited.Contains(dependencyName) && queued.Add(dependencyName))
                        queue.Enqueue(new DiscoveryEntry(dependency, namespaceName, location));
                }
            }
        }
    }

    private static FixtureGenerationSpec? CreateConfiguration(
        Compilation compilation,
        INamedTypeSymbol type,
        string namespaceName,
        string configurationName,
        string recipeName,
        Location? location,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var typeName = Display(type);
        var operations = ConstructionOperationDiscovery.Discover(
            type,
            compilation.Assembly,
            diagnostics,
            location);
        var inferredValues = ConventionalValueExpressionDiscovery.DiscoverReferencedValues(
            type,
            compilation.Assembly);
        var selfValue = ConventionalValueExpressionDiscovery.DiscoverValue(
                type,
                compilation.Assembly)
            .FirstOrDefault(value => value.TypeName == typeName);
        if (operations.IsEmpty && selfValue is null)
            return null;

        var properties = FluentFixtureConfigurationProvider.DiscoverProperties(
            type,
            compilation.Assembly);
        var constructors = FluentFixtureConfigurationProvider.DiscoverReconstructionConstructors(
            type,
            compilation.Assembly);
        return new FixtureGenerationSpec(
            configurationName,
            namespaceName,
            recipeName,
            typeName,
            type.Name,
            operations.IsEmpty ? selfValue!.Expression : string.Empty,
            validatorFactoryExpression: null,
            validationRulesTypeKey: null,
            properties,
            type.IsRecord,
            constructors,
            FluentFixtureConfigurationProvider.CanUseDerivedReconstruction(
                type,
                compilation.Assembly),
            FluentFixtureConfigurationProvider.HasValueEqualitySemantics(type),
            FluentFixtureConfigurationProvider.DiscoverEquivalentCopyExpression(
                type,
                compilation.Assembly,
                constructors),
            operations,
            location,
            FluentFixtureConfigurationProvider.CanExposePublicFactory(type),
            usesSynthesizedBaseline: !operations.IsEmpty,
            inferredValues: inferredValues);
    }

    private static string ConfigurationIdentity(FixtureGenerationSpec configuration) =>
        configuration.NamespaceName + "|" + configuration.ConfigurationName + "|" +
        configuration.RecipeName;

    private static INamedTypeSymbol? ResolveType(Compilation compilation, string typeName)
    {
        const string globalPrefix = "global::";
        var metadataName = typeName.StartsWith(globalPrefix, StringComparison.Ordinal)
            ? typeName.Substring(globalPrefix.Length)
            : typeName;
        return compilation.GetTypeByMetadataName(metadataName);
    }

    private static IEnumerable<IPropertySymbol> EnumerateReadableProperties(
        INamedTypeSymbol type,
        IAssemblySymbol currentAssembly)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!property.IsStatic &&
                    property.Parameters.Length == 0 &&
                    names.Add(property.Name) &&
                    property.GetMethod is not null &&
                    IsAccessible(property.GetMethod, currentAssembly))
                {
                    yield return property;
                }
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> ExpandDomainTypes(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            foreach (var element in ExpandDomainTypes(array.ElementType))
                yield return element;
            yield break;
        }

        if (type is not INamedTypeSymbol named)
            yield break;

        if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            foreach (var argument in ExpandDomainTypes(named.TypeArguments[0]))
                yield return argument;
            yield break;
        }

        if (named.TypeArguments.Length > 0)
        {
            foreach (var argument in named.TypeArguments)
            {
                foreach (var dependency in ExpandDomainTypes(argument))
                    yield return dependency;
            }
        }

        var namespaceName = named.ContainingNamespace.ToDisplayString();
        if (named.SpecialType != SpecialType.None ||
            namespaceName == "System" ||
            namespaceName.StartsWith("System.", StringComparison.Ordinal) ||
            named.TypeKind is TypeKind.Enum or TypeKind.Interface or TypeKind.Delegate ||
            named.IsAbstract ||
            named.IsUnboundGenericType ||
            named.TypeArguments.Any(argument => argument.TypeKind == TypeKind.TypeParameter))
        {
            yield break;
        }

        yield return named;
    }

    private static string CreateConfigurationName(
        INamedTypeSymbol type,
        string namespaceName,
        ISet<string> occupiedNames)
    {
        var baseName = Sanitize(type.Name) + "Fixture";
        if (occupiedNames.Add(namespaceName + "|" + baseName))
            return baseName;

        var namespaceParts = type.ContainingNamespace.ToDisplayString()
            .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Reverse()
            .Select(Sanitize)
            .ToArray();
        var prefix = string.Empty;
        foreach (var part in namespaceParts)
        {
            prefix = part + prefix;
            var candidate = prefix + baseName;
            if (occupiedNames.Add(namespaceName + "|" + candidate))
                return candidate;
        }

        var suffix = 2;
        while (!occupiedNames.Add(namespaceName + "|" + baseName + suffix))
            suffix++;
        return baseName + suffix;
    }

    private static string Sanitize(string value)
    {
        var characters = value.Where(char.IsLetterOrDigit).ToArray();
        return characters.Length == 0 ? "DomainType" : new string(characters);
    }

    private static bool IsAccessible(IMethodSymbol method, IAssemblySymbol currentAssembly)
    {
        var sameAssembly = SymbolEqualityComparer.Default.Equals(
            method.ContainingAssembly,
            currentAssembly);
        return method.DeclaredAccessibility == Accessibility.Public ||
               sameAssembly && method.DeclaredAccessibility is
                   Accessibility.Internal or Accessibility.ProtectedOrInternal;
    }

    private static string Display(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private sealed class DiscoveryEntry
    {
        public INamedTypeSymbol Type { get; }
        public string NamespaceName { get; }
        public Location? Location { get; }

        public DiscoveryEntry(
            INamedTypeSymbol type,
            string namespaceName,
            Location? location)
        {
            Type = type;
            NamespaceName = namespaceName;
            Location = location;
        }
    }
}
