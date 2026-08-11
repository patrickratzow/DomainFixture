using System.Collections.Generic;
using System.Collections.Immutable;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Extraction;

internal static class ValidationRuleManifestProvider
{
    private const string DomainContractAttributeMetadataName =
        "DomainFixture.Generation.Metadata.DomainContractManifestAttribute";
    private const string LegacyAttributeMetadataName =
        "DomainFixture.Generation.Metadata.ValidationRuleManifestAttribute";

    public static IncrementalValueProvider<ImmutableArray<ConstraintExtractionResult>> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider.Select(static (compilation, _) => Extract(compilation));
    }

    private static ImmutableArray<ConstraintExtractionResult> Extract(Compilation compilation)
    {
        var results = ImmutableArray.CreateBuilder<ConstraintExtractionResult>();
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
                continue;

            foreach (var attribute in assembly.GetAttributes())
            {
                var metadataName = attribute.AttributeClass?.ToDisplayString();
                if (metadataName == DomainContractAttributeMetadataName)
                {
                    var result = ReadDomainContract(attribute);
                    if (result is not null)
                        results.Add(result);
                }
                else if (metadataName == LegacyAttributeMetadataName)
                {
                    var result = ReadLegacyContract(attribute);
                    if (result is not null)
                        results.Add(result);
                }
            }
        }

        return results.ToImmutable();
    }

    private static ConstraintExtractionResult? ReadDomainContract(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length != 8 ||
            attribute.ConstructorArguments[0].Value is not int schemaVersion ||
            attribute.ConstructorArguments[1].Value is not INamedTypeSymbol sourceType ||
            attribute.ConstructorArguments[2].Value is not INamedTypeSymbol subjectType ||
            attribute.ConstructorArguments[3].Value is not string memberPath ||
            attribute.ConstructorArguments[4].Value is not string kindId ||
            attribute.ConstructorArguments[7].Value is not bool propertyCanBeAssigned)
        {
            return null;
        }

        if (schemaVersion != DomainConstraintContract.CurrentSchemaVersion)
        {
            return ConstraintExtractionResult.Failure(
                GeneratorDiagnostics.DomainContractSchemaUnsupported(
                    location: null,
                    schemaVersion,
                    kindId));
        }

        var parameters = new Dictionary<string, string>();
        foreach (var item in attribute.ConstructorArguments[5].Values)
        {
            if (item.Value is not string pair)
                continue;

            var separator = pair.IndexOf('=');
            if (separator <= 0)
                continue;

            parameters[pair.Substring(0, separator)] = pair.Substring(separator + 1);
        }

        var contract = new DomainConstraintContract(
            kindId,
            sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            memberPath,
            parameters,
            attribute.ConstructorArguments[6].Value as string,
            schemaVersion);
        return ConstraintExtractionResult.Success(new DiscoveredDomainConstraint(
            contract,
            propertyCanBeAssigned,
            location: null));
    }

    private static ConstraintExtractionResult? ReadLegacyContract(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length != 8 ||
            attribute.ConstructorArguments[0].Value is not INamedTypeSymbol sourceType ||
            attribute.ConstructorArguments[1].Value is not INamedTypeSymbol subjectType ||
            attribute.ConstructorArguments[2].Value is not string memberPath ||
            attribute.ConstructorArguments[3].Value is not int rawKind ||
            attribute.ConstructorArguments[4].Value is not int minimum ||
            attribute.ConstructorArguments[5].Value is not int maximum ||
            attribute.ConstructorArguments[7].Value is not bool propertyCanBeAssigned ||
            !TryMapLegacyKind(rawKind, out var kindId))
        {
            return null;
        }

        return ConstraintExtractionResult.Success(new DiscoveredDomainConstraint(
            sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            memberPath,
            kindId,
            minimum < 0 ? null : minimum,
            maximum < 0 ? null : maximum,
            attribute.ConstructorArguments[6].Value as string,
            propertyCanBeAssigned,
            location: null));
    }

    private static bool TryMapLegacyKind(int rawKind, out string kindId)
    {
        kindId = rawKind switch
        {
            0 => DomainConstraintKinds.TextLength,
            1 => DomainConstraintKinds.TextNotEmpty,
            2 => DomainConstraintKinds.TextNotNull,
            3 => DomainConstraintKinds.TextMaximumLength,
            4 => DomainConstraintKinds.TextMinimumLength,
            5 => DomainConstraintKinds.Int32InclusiveRange,
            6 => DomainConstraintKinds.Int32ExclusiveRange,
            7 => DomainConstraintKinds.Int32GreaterThan,
            8 => DomainConstraintKinds.Int32LessThan,
            _ => string.Empty
        };
        return kindId.Length > 0;
    }
}
