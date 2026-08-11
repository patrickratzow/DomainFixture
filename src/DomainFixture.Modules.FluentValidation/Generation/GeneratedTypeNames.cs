using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DomainFixture.Modules.FluentValidation;

internal static class GeneratedTypeNames
{
    public static string Display(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string AdapterClassName(
        INamedTypeSymbol subjectType,
        INamedTypeSymbol validatorType)
    {
        var validatorTypeName = Display(validatorType);
        return Sanitize(subjectType.Name) + Sanitize(validatorType.Name) + "Adapter_" +
               StableHash(validatorTypeName);
    }

    private static string Sanitize(string value)
    {
        var characters = value.Where(char.IsLetterOrDigit).ToArray();
        return characters.Length == 0 ? "Type" : new string(characters);
    }

    private static string StableHash(string value)
    {
        unchecked
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var hash = offsetBasis;
            foreach (var character in value)
            {
                hash ^= character;
                hash *= prime;
            }
            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
