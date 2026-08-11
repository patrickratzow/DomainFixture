using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Diagnostics;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Discovery;

internal static class ConstructionOperationDiscovery
{
    private static readonly HashSet<string> FactoryNames = new(StringComparer.Ordinal)
    {
        "From",
        "Create",
        "Of"
    };

    public static ImmutableArray<DomainOperationContract> Discover(
        ITypeSymbol subjectType,
        IAssemblySymbol currentAssembly,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        Location? fallbackLocation)
    {
        if (subjectType is not INamedTypeSymbol namedType)
            return ImmutableArray<DomainOperationContract>.Empty;

        var readableProperties = EnumerateProperties(namedType)
            .Where(property =>
                property.GetMethod is not null &&
                IsAccessibleFromGeneratedCode(property.GetMethod, currentAssembly))
            .ToArray();
        var operations = ImmutableArray.CreateBuilder<DomainOperationContract>();
        var operationKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var constructor in namedType.InstanceConstructors.Where(constructor =>
                     constructor.Parameters.Length > 0 &&
                     IsAccessibleFromGeneratedCode(constructor, currentAssembly)))
        {
            AddOperation(
                constructor,
                DomainOperationKinds.Constructor,
                namedType,
                subjectType,
                readableProperties,
                diagnostics,
                fallbackLocation,
                operations,
                operationKeys);
        }

        for (var current = namedType; current is not null; current = current.BaseType)
        {
            foreach (var factory in current.GetMembers().OfType<IMethodSymbol>().Where(method =>
                         method.IsStatic &&
                         method.MethodKind == MethodKind.Ordinary &&
                         method.TypeParameters.Length == 0 &&
                         FactoryNames.Contains(method.Name) &&
                         method.Parameters.Length > 0 &&
                         IsAccessibleFromGeneratedCode(method, currentAssembly)))
            {
                AddOperation(
                    factory,
                    DomainOperationKinds.StaticFactory,
                    factory.ContainingType,
                    subjectType,
                    readableProperties,
                    diagnostics,
                    fallbackLocation,
                    operations,
                    operationKeys);
            }
        }

        return operations.ToImmutable();
    }

    private static void AddOperation(
        IMethodSymbol method,
        string kindId,
        INamedTypeSymbol declaringType,
        ITypeSymbol subjectType,
        IReadOnlyList<IPropertySymbol> readableProperties,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        Location? fallbackLocation,
        ImmutableArray<DomainOperationContract>.Builder operations,
        ISet<string> operationKeys)
    {
        var mappedParameters = MapParameters(
            method,
            subjectType,
            readableProperties,
            diagnostics,
            fallbackLocation);
        if (mappedParameters is null)
            return;

        var declaringTypeName = declaringType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var subjectTypeName = subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var memberName = method.MethodKind == MethodKind.Constructor ? ".ctor" : method.Name;
        var operationKey = $"{kindId}|{declaringTypeName}|{memberName}|{string.Join("|", mappedParameters.Select(parameter => parameter.TypeName))}";
        if (!operationKeys.Add(operationKey))
            return;

        var returnTypeName = method.MethodKind == MethodKind.Constructor
            ? subjectTypeName
            : method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        operations.Add(new DomainOperationContract(
            DomainOperationContract.CreateOperationId(
                kindId,
                declaringTypeName,
                memberName,
                mappedParameters),
            kindId,
            declaringTypeName,
            subjectTypeName,
            memberName,
            returnTypeName,
            mappedParameters));
    }

    private static IReadOnlyList<DomainOperationParameterContract>? MapParameters(
        IMethodSymbol method,
        ITypeSymbol subjectType,
        IReadOnlyList<IPropertySymbol> readableProperties,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        Location? fallbackLocation)
    {
        var availableProperties = new List<IPropertySymbol>(readableProperties);
        var mappedParameters = new List<DomainOperationParameterContract>(method.Parameters.Length);

        foreach (var parameter in method.Parameters)
        {
            var typeMatches = availableProperties
                .Where(property => SymbolEqualityComparer.Default.Equals(property.Type, parameter.Type))
                .ToArray();
            var nameMatches = typeMatches
                .Where(property => string.Equals(
                    property.Name,
                    parameter.Name,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            IPropertySymbol? property;
            if (nameMatches.Length == 1)
            {
                property = nameMatches[0];
            }
            else if (nameMatches.Length > 1 || typeMatches.Length > 1)
            {
                diagnostics.Add(GeneratorDiagnostics.OperationParameterMappingAmbiguous(
                    method.Locations.FirstOrDefault() ?? fallbackLocation,
                    method.ToDisplayString(),
                    subjectType.ToDisplayString(),
                    parameter.Name));
                return null;
            }
            else
            {
                property = typeMatches.SingleOrDefault();
            }

            if (property is null)
                return null;

            availableProperties.Remove(property);
            mappedParameters.Add(new DomainOperationParameterContract(
                parameter.Name,
                parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                property.Name));
        }

        return mappedParameters;
    }

    private static IEnumerable<IPropertySymbol> EnumerateProperties(INamedTypeSymbol subjectType)
    {
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        for (var current = subjectType; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!property.IsStatic &&
                    property.Parameters.Length == 0 &&
                    seenNames.Add(property.Name))
                {
                    yield return property;
                }
            }
        }
    }

    private static bool IsAccessibleFromGeneratedCode(
        IMethodSymbol method,
        IAssemblySymbol currentAssembly)
    {
        var sameAssembly = SymbolEqualityComparer.Default.Equals(
            method.ContainingAssembly,
            currentAssembly);
        return method.DeclaredAccessibility == Accessibility.Public ||
               sameAssembly && method.DeclaredAccessibility is Accessibility.Internal or
                   Accessibility.ProtectedOrInternal;
    }
}
