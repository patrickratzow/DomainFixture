using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.TestGenerator.Modules;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Extraction;

internal static class DomainFixtureModuleManifestProvider
{
    private const string ModuleAttributeMetadataName =
        "DomainFixture.Generation.Metadata.DomainFixtureModuleManifestAttribute";
    private const string RecipeAttributeMetadataName =
        "DomainFixture.Generation.Metadata.DomainFixtureRecipeManifestAttribute";
    private const string ValidationAttributeMetadataName =
        "DomainFixture.Generation.Metadata.DomainFixtureValidationManifestAttribute";

    public static IncrementalValueProvider<CompileTimeModuleCatalog> Create(
        IncrementalGeneratorInitializationContext context) =>
        context.CompilationProvider.Select(static (compilation, _) => Extract(compilation));

    internal static CompileTimeModuleCatalog Extract(Compilation compilation)
    {
        var modules = ImmutableArray.CreateBuilder<CompileTimeModuleDescriptor>();
        var recipes = ImmutableArray.CreateBuilder<CompileTimeRecipeContribution>();
        var validations = ImmutableArray.CreateBuilder<CompileTimeValidationContribution>();

        foreach (var assembly in EnumerateAssemblies(compilation))
        {
            foreach (var attribute in assembly.GetAttributes())
            {
                var metadataName = attribute.AttributeClass?.ToDisplayString();
                if (metadataName == ModuleAttributeMetadataName)
                    ExtractModule(attribute, modules);
                else if (metadataName == RecipeAttributeMetadataName)
                    ExtractRecipe(attribute, recipes);
                else if (metadataName == ValidationAttributeMetadataName)
                    ExtractValidation(attribute, validations);
            }
        }

        return CompileTimeModuleCatalog.Create(modules, recipes, validations);
    }

    private static IEnumerable<IAssemblySymbol> EnumerateAssemblies(Compilation compilation)
    {
        yield return compilation.Assembly;
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                yield return assembly;
        }
    }

    private static void ExtractModule(
        AttributeData attribute,
        ImmutableArray<CompileTimeModuleDescriptor>.Builder modules)
    {
        var arguments = attribute.ConstructorArguments;
        if (arguments.Length != 5)
        {
            modules.Add(new CompileTimeModuleDescriptor(0, string.Empty, string.Empty, string.Empty, new string[0]));
            return;
        }

        modules.Add(new CompileTimeModuleDescriptor(
            arguments[0].Value as int? ?? 0,
            arguments[1].Value as string ?? string.Empty,
            arguments[2].Value as string ?? string.Empty,
            arguments[3].Value is INamedTypeSymbol sourceType
                ? sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : string.Empty,
            arguments[4].Values
                .Select(value => value.Value as string)
                .Where(value => value is not null)
                .Cast<string>()));
    }

    private static void ExtractRecipe(
        AttributeData attribute,
        ImmutableArray<CompileTimeRecipeContribution>.Builder recipes)
    {
        var arguments = attribute.ConstructorArguments;
        if (arguments.Length != 7)
        {
            recipes.Add(new CompileTimeRecipeContribution(
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty));
            return;
        }

        recipes.Add(new CompileTimeRecipeContribution(
            arguments[0].Value as int? ?? 0,
            arguments[1].Value as string ?? string.Empty,
            arguments[2].Value is INamedTypeSymbol sourceType
                ? sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : string.Empty,
            arguments[3].Value is INamedTypeSymbol subjectType
                ? subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : string.Empty,
            arguments[4].Value as string ?? string.Empty,
            arguments[5].Value as string ?? string.Empty,
            arguments[6].Value as string ?? string.Empty));
    }

    private static void ExtractValidation(
        AttributeData attribute,
        ImmutableArray<CompileTimeValidationContribution>.Builder validations)
    {
        var arguments = attribute.ConstructorArguments;
        if (arguments.Length != 5)
        {
            validations.Add(new CompileTimeValidationContribution(
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty));
            return;
        }

        validations.Add(new CompileTimeValidationContribution(
            arguments[0].Value as int? ?? 0,
            arguments[1].Value as string ?? string.Empty,
            arguments[2].Value is INamedTypeSymbol sourceType
                ? sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : string.Empty,
            arguments[3].Value is INamedTypeSymbol subjectType
                ? subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : string.Empty,
            arguments[4].Value as string ?? string.Empty));
    }
}
