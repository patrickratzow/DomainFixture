using System.Collections.Generic;
using System.Collections.Immutable;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Extraction;

internal static class DomainScenarioManifestProvider
{
    private const string AttributeMetadataName =
        "DomainFixture.Generation.Metadata.DomainScenarioManifestAttribute";

    public static IncrementalValueProvider<ImmutableArray<ScenarioExtractionResult>> Create(
        IncrementalGeneratorInitializationContext context) =>
        context.CompilationProvider.Select(static (compilation, _) => Extract(compilation));

    private static ImmutableArray<ScenarioExtractionResult> Extract(Compilation compilation)
    {
        var results = ImmutableArray.CreateBuilder<ScenarioExtractionResult>();
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
                continue;

            foreach (var attribute in assembly.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != AttributeMetadataName ||
                    attribute.ConstructorArguments.Length != 5 ||
                    attribute.ConstructorArguments[0].Value is not int schemaVersion ||
                    attribute.ConstructorArguments[1].Value is not INamedTypeSymbol sourceType ||
                    attribute.ConstructorArguments[2].Value is not INamedTypeSymbol subjectType ||
                    attribute.ConstructorArguments[3].Value is not string kindId)
                {
                    continue;
                }

                if (schemaVersion != DomainScenarioContract.CurrentSchemaVersion)
                {
                    results.Add(ScenarioExtractionResult.Failure(
                        GeneratorDiagnostics.DomainScenarioSchemaUnsupported(
                            location: null,
                            schemaVersion,
                            kindId)));
                    continue;
                }

                var parameters = new Dictionary<string, string>();
                foreach (var item in attribute.ConstructorArguments[4].Values)
                {
                    if (item.Value is not string pair)
                        continue;

                    var separator = pair.IndexOf('=');
                    if (separator > 0)
                        parameters[pair.Substring(0, separator)] = pair.Substring(separator + 1);
                }

                results.Add(ScenarioExtractionResult.Success(new DomainScenarioContract(
                    kindId,
                    sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    parameters,
                    schemaVersion)));
            }
        }

        return results.ToImmutable();
    }
}
