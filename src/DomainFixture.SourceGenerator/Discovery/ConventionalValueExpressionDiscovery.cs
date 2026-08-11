using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.SourceGenerator.Models;
using DomainFixture.SourceGenerator.Emission;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DomainFixture.SourceGenerator.Discovery;

internal static class ConventionalValueExpressionDiscovery
{
    private static readonly HashSet<string> ConventionalFactoryNames =
        new(StringComparer.Ordinal) { "From", "Create", "Of" };

    public static ImmutableArray<InferredValueSpec> DiscoverReferencedValues(
        ITypeSymbol subjectType,
        IAssemblySymbol currentAssembly)
    {
        var values = new Dictionary<string, InferredValueSpec>(StringComparer.Ordinal);
        if (subjectType is not INamedTypeSymbol namedSubject)
            return ImmutableArray<InferredValueSpec>.Empty;

        foreach (var property in EnumerateReadableProperties(namedSubject, currentAssembly))
        {
            DiscoverType(
                property.Type,
                currentAssembly,
                values,
                new HashSet<string>(StringComparer.Ordinal));
        }

        return values.Values
            .OrderBy(value => value.TypeName, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public static ImmutableArray<InferredValueSpec> DiscoverValue(
        ITypeSymbol valueType,
        IAssemblySymbol currentAssembly)
    {
        var values = new Dictionary<string, InferredValueSpec>(StringComparer.Ordinal);
        DiscoverType(
            valueType,
            currentAssembly,
            values,
            new HashSet<string>(StringComparer.Ordinal));
        return values.Values
            .OrderBy(value => value.TypeName, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static string? DiscoverType(
        ITypeSymbol type,
        IAssemblySymbol currentAssembly,
        IDictionary<string, InferredValueSpec> values,
        ISet<string> activeTypes)
    {
        type = UnwrapNullable(type);
        if (TryPrimitive(type, out var primitive))
            return primitive;

        if (type is IArrayTypeSymbol array)
        {
            var element = DiscoverType(array.ElementType, currentAssembly, values, activeTypes);
            return element is null
                ? null
                : $"new {Display(array.ElementType)}[] {{ {element} }}";
        }

        if (type is not INamedTypeSymbol namedType)
            return null;

        if (namedType.TypeKind == TypeKind.Enum)
        {
            var member = namedType.GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(field => field.HasConstantValue && !field.IsImplicitlyDeclared);
            if (member is null)
                return null;

            var enumExpression = $"{Display(namedType)}.{Escape(member.Name)}";
            AddValue(values, namedType, enumExpression, $"enum:{member.Name}", member.Locations.FirstOrDefault());
            return enumExpression;
        }

        if (TryCollection(namedType, currentAssembly, values, activeTypes, out var collection))
            return collection;

        var namespaceName = namedType.ContainingNamespace.ToDisplayString();
        if (namespaceName == "System" ||
            namespaceName.StartsWith("System.", StringComparison.Ordinal))
        {
            return null;
        }

        var typeName = Display(namedType);
        if (values.TryGetValue(typeName, out var existing))
            return existing.Expression;
        if (!activeTypes.Add(typeName))
            return null;

        var candidates = FactoryCandidates(namedType, currentAssembly)
            .Concat(ConstructorCandidates(namedType, currentAssembly));
        foreach (var candidate in candidates)
        {
            var arguments = new List<string>();
            var covered = true;
            foreach (var parameter in candidate.Parameters)
            {
                var argument = DiscoverType(
                    parameter.Type,
                    currentAssembly,
                    values,
                    activeTypes);
                if (argument is null)
                {
                    covered = false;
                    break;
                }

                arguments.Add(argument);
            }

            if (!covered)
                continue;

            var expression = candidate.MethodKind == MethodKind.Constructor
                ? $"new {typeName}({string.Join(", ", arguments)})"
                : $"{Display(candidate.ContainingType)}.{Escape(candidate.Name)}({string.Join(", ", arguments)})";
            AddValue(
                values,
                namedType,
                expression,
                candidate.ToDisplayString(),
                candidate.Locations.FirstOrDefault());
            activeTypes.Remove(typeName);
            return expression;
        }

        activeTypes.Remove(typeName);
        return null;
    }

    private static IEnumerable<IMethodSymbol> FactoryCandidates(
        INamedTypeSymbol type,
        IAssemblySymbol currentAssembly)
    {
        var factories = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method =>
                method.IsStatic &&
                method.MethodKind == MethodKind.Ordinary &&
                !method.IsGenericMethod &&
                method.Parameters.Length > 0 &&
                SymbolEqualityComparer.Default.Equals(method.ReturnType, type) &&
                IsAccessible(method, currentAssembly))
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ThenBy(method => method.Parameters.Length)
            .ToArray();
        var conventional = factories
            .Where(method => ConventionalFactoryNames.Contains(method.Name))
            .ToArray();
        if (conventional.Length > 0)
            return conventional;

        return factories.Length == 1
            ? factories
            : Array.Empty<IMethodSymbol>();
    }

    private static IEnumerable<IMethodSymbol> ConstructorCandidates(
        INamedTypeSymbol type,
        IAssemblySymbol currentAssembly) =>
        type.InstanceConstructors
            .Where(constructor =>
                constructor.Parameters.Length > 0 &&
                IsAccessible(constructor, currentAssembly))
            .OrderBy(constructor => constructor.Parameters.Length);

    private static bool TryCollection(
        INamedTypeSymbol type,
        IAssemblySymbol currentAssembly,
        IDictionary<string, InferredValueSpec> values,
        ISet<string> activeTypes,
        out string? expression)
    {
        expression = null;
        if (type.TypeArguments.Length != 1 || !IsCollection(type))
            return false;

        var elementType = type.TypeArguments[0];
        var element = DiscoverType(elementType, currentAssembly, values, activeTypes);
        if (element is null)
            return true;

        var definition = type.OriginalDefinition.ToDisplayString();
        expression = definition.EndsWith("HashSet<T>", StringComparison.Ordinal) ||
                     definition.EndsWith("ISet<T>", StringComparison.Ordinal)
            ? $"new global::System.Collections.Generic.HashSet<{Display(elementType)}> {{ {element} }}"
            : definition.EndsWith("List<T>", StringComparison.Ordinal) ||
              definition.EndsWith("IList<T>", StringComparison.Ordinal) ||
              definition.EndsWith("ICollection<T>", StringComparison.Ordinal)
                ? $"new global::System.Collections.Generic.List<{Display(elementType)}> {{ {element} }}"
                : $"new {Display(elementType)}[] {{ {element} }}";
        return true;
    }

    private static bool IsCollection(INamedTypeSymbol type)
    {
        var definition = type.OriginalDefinition.ToDisplayString();
        return definition is
            "System.Collections.Generic.IEnumerable<T>" or
            "System.Collections.Generic.IReadOnlyCollection<T>" or
            "System.Collections.Generic.IReadOnlyList<T>" or
            "System.Collections.Generic.List<T>" or
            "System.Collections.Generic.IList<T>" or
            "System.Collections.Generic.ICollection<T>" or
            "System.Collections.Generic.HashSet<T>" or
            "System.Collections.Generic.ISet<T>";
    }

    private static bool TryPrimitive(ITypeSymbol type, out string? expression)
    {
        expression = type.SpecialType switch
        {
            SpecialType.System_String =>
                $"{UniqueValueSourceEmitter.StringFactoryExpression}(1, 2147483647)",
            SpecialType.System_Boolean => "true",
            SpecialType.System_Byte => "1",
            SpecialType.System_SByte => "1",
            SpecialType.System_Int16 => "1",
            SpecialType.System_UInt16 => "1",
            SpecialType.System_Int32 => "1",
            SpecialType.System_UInt32 => "1U",
            SpecialType.System_Int64 => "1L",
            SpecialType.System_UInt64 => "1UL",
            SpecialType.System_Single => "1F",
            SpecialType.System_Double => "1D",
            SpecialType.System_Decimal => "1M",
            SpecialType.System_Char => SymbolDisplay.FormatLiteral('a', quote: true),
            _ => null
        };
        if (expression is not null)
            return true;

        if (Display(type) == "global::System.Guid")
        {
            expression = "global::System.Guid.NewGuid()";
            return true;
        }

        return false;
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol
        {
            OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
            TypeArguments.Length: 1
        } nullable
            ? nullable.TypeArguments[0]
            : type;

    private static IEnumerable<IPropertySymbol> EnumerateReadableProperties(
        INamedTypeSymbol type,
        IAssemblySymbol currentAssembly)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!property.IsStatic &&
                    property.Parameters.Length == 0 &&
                    property.GetMethod is not null &&
                    IsAccessible(property.GetMethod, currentAssembly))
                {
                    yield return property;
                }
            }
        }
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

    private static void AddValue(
        IDictionary<string, InferredValueSpec> values,
        ITypeSymbol type,
        string expression,
        string sourceId,
        Location? location)
    {
        var typeName = Display(type);
        if (!values.ContainsKey(typeName))
            values.Add(typeName, new InferredValueSpec(typeName, expression, sourceId, location));
    }

    private static string Display(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string Escape(string identifier) =>
        SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
        SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;
}
