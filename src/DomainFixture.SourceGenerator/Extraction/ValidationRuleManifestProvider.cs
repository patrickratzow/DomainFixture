using System.Collections.Immutable;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Extraction;

internal static class ValidationRuleManifestProvider
{
    private const string AttributeMetadataName =
        "DomainFixture.Generation.Metadata.ValidationRuleManifestAttribute";

    public static IncrementalValueProvider<ImmutableArray<RuleExtractionResult>> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider.Select(static (compilation, _) => Extract(compilation));
    }

    private static ImmutableArray<RuleExtractionResult> Extract(Compilation compilation)
    {
        var results = ImmutableArray.CreateBuilder<RuleExtractionResult>();
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
            {
                continue;
            }

            foreach (var attribute in assembly.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != AttributeMetadataName ||
                    attribute.ConstructorArguments.Length != 8 ||
                    attribute.ConstructorArguments[0].Value is not INamedTypeSymbol rulesType ||
                    attribute.ConstructorArguments[1].Value is not INamedTypeSymbol subjectType ||
                    attribute.ConstructorArguments[2].Value is not string propertyName ||
                    attribute.ConstructorArguments[3].Value is not int rawKind ||
                    attribute.ConstructorArguments[4].Value is not int minimum ||
                    attribute.ConstructorArguments[5].Value is not int maximum ||
                    attribute.ConstructorArguments[7].Value is not bool propertyCanBeAssigned)
                {
                    continue;
                }

                if (!TryMapKind(rawKind, out var kind))
                    continue;

                results.Add(RuleExtractionResult.Success(new ValidationRuleSpec(
                    rulesType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    propertyName,
                    kind,
                    minimum < 0 ? null : minimum,
                    maximum < 0 ? null : maximum,
                    attribute.ConstructorArguments[6].Value as string,
                    propertyCanBeAssigned,
                    null)));
            }
        }

        return results.ToImmutable();
    }

    private static bool TryMapKind(int rawKind, out ValidationRuleKind kind)
    {
        if (rawKind >= (int)ValidationRuleKind.StringLength &&
            rawKind <= (int)ValidationRuleKind.StringMaximumLength)
        {
            kind = (ValidationRuleKind)rawKind;
            return true;
        }

        kind = default;
        return false;
    }
}
