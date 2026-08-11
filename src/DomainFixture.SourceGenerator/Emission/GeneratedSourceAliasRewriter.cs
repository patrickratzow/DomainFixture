using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainFixture.SourceGenerator.Emission;

internal static class GeneratedSourceAliasRewriter
{
    public static string Rewrite(
        string source,
        IEnumerable<string> typeNames,
        string generatedNamespace)
    {
        var root = ParseCompilationUnit(source);
        var blocked = CollectBlockedIdentifiers(root);
        var declaredTypes = CollectDeclaredTypeIdentifiers(root);
        var normalizedTypeNames = typeNames
            .Where(typeName => !string.IsNullOrWhiteSpace(typeName))
            .Select(NormalizeTypeName)
            .Where(typeName => typeName.StartsWith("global::", StringComparison.Ordinal))
            .Where(source.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var references = normalizedTypeNames
            .Where(typeName => !IsFrameworkType(typeName))
            .OrderByDescending(typeName => typeName.Length)
            .ThenBy(typeName => typeName, StringComparer.Ordinal)
            .ToArray();
        var frameworkReferences = normalizedTypeNames
            .Where(IsFrameworkType)
            .ToArray();

        var aliases = PlanAliases(references, generatedNamespace, blocked, declaredTypes);
        foreach (var alias in aliases.OrderByDescending(candidate => candidate.TypeName.Length))
            source = ReplaceTypeReference(source, alias.TypeName, alias.Replacement);

        var namespaceUsings = new HashSet<string>(StringComparer.Ordinal);
        source = SimplifyFrameworkNames(
            source,
            blocked,
            aliases,
            frameworkReferences,
            namespaceUsings);
        root = ParseCompilationUnit(source);

        var existingNamespaces = new HashSet<string>(
            root.Usings
                .Where(usingDirective => usingDirective.Alias is null && !usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                .Select(usingDirective => usingDirective.Name?.ToString() ?? string.Empty),
            StringComparer.Ordinal);
        var normalUsings = root.Usings.ToList();
        foreach (var namespaceName in namespaceUsings.OrderBy(value => value, StringComparer.Ordinal))
        {
            if (existingNamespaces.Add(namespaceName))
                normalUsings.Add(UsingDirective(ParseName(namespaceName)));
        }

        var aliasUsings = aliases
            .Where(alias => alias.RequiresUsing)
            .OrderBy(alias => alias.Replacement.TrimStart('@'), StringComparer.Ordinal)
            .Select(alias => UsingDirective(ParseName(alias.TypeName))
                .WithAlias(NameEquals(IdentifierName(alias.Replacement))))
            .ToArray();
        root = root.WithUsings(List(normalUsings.Concat(aliasUsings)));
        return root.NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString() + "\n";
    }

    private static IReadOnlyList<AliasPlan> PlanAliases(
        IReadOnlyList<string> typeNames,
        string generatedNamespace,
        ISet<string> blocked,
        ISet<string> declaredTypes)
    {
        var preliminary = typeNames.Select(typeName => new AliasCandidate(
            typeName,
            GetSimpleName(typeName),
            GetOuterNamespace(typeName))).ToArray();
        var conflicts = preliminary
            .GroupBy(candidate => candidate.SimpleName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var used = new HashSet<string>(blocked, StringComparer.Ordinal);
        var plans = new List<AliasPlan>(preliminary.Length);

        foreach (var candidate in preliminary
                     .OrderBy(value => value.SimpleName, StringComparer.Ordinal)
                     .ThenBy(value => value.TypeName, StringComparer.Ordinal))
        {
            if (candidate.NamespaceName == generatedNamespace &&
                conflicts[candidate.SimpleName].Length == 1 &&
                (!used.Contains(candidate.SimpleName) || declaredTypes.Contains(candidate.SimpleName)))
            {
                plans.Add(new AliasPlan(candidate.TypeName, EscapeIdentifier(candidate.SimpleName), false));
                used.Add(candidate.SimpleName);
                continue;
            }

            var alias = candidate.SimpleName;
            if (conflicts[alias].Length > 1)
            {
                alias = CreateConflictAlias(candidate, conflicts[alias], used);
            }
            else if (used.Contains(alias))
            {
                alias = CreateQualifiedAlias(candidate.NamespaceName, alias, used);
            }
            alias = EscapeIdentifier(alias);
            used.Add(alias.TrimStart('@'));
            plans.Add(new AliasPlan(candidate.TypeName, alias, true));
        }

        return plans;
    }

    private static string SimplifyFrameworkNames(
        string source,
        ISet<string> blocked,
        IReadOnlyList<AliasPlan> aliases,
        IReadOnlyCollection<string> frameworkReferences,
        ISet<string> namespaceUsings)
    {
        var usedAliases = new HashSet<string>(
            aliases.Select(alias => alias.Replacement.TrimStart('@')),
            StringComparer.Ordinal);
        source = ReplaceFrameworkType(
            source, "global::NUnit.Framework.Assert", "Assert", "NUnit.Framework", blocked, usedAliases, namespaceUsings);
        source = ReplaceFrameworkType(
            source, "global::NUnit.Framework.Is", "Is", "NUnit.Framework", blocked, usedAliases, namespaceUsings);
        source = ReplaceFrameworkType(
            source, "global::System.ArgumentNullException", "ArgumentNullException", "System", blocked, usedAliases, namespaceUsings);
        source = ReplaceFrameworkType(
            source, "global::System.InvalidOperationException", "InvalidOperationException", "System", blocked, usedAliases, namespaceUsings);
        source = ReplaceFrameworkType(
            source, "global::System.Guid", "Guid", "System", blocked, usedAliases, namespaceUsings);

        foreach (var typeName in frameworkReferences
                     .Where(typeName => typeName.StartsWith("global::System.", StringComparison.Ordinal))
                     .Where(typeName => typeName.IndexOfAny(new[] { '<', '[', '*' }) < 0)
                     .OrderByDescending(typeName => typeName.Length))
        {
            var simpleName = GetSimpleName(typeName);
            if (blocked.Contains(simpleName) || usedAliases.Contains(simpleName))
                continue;

            source = ReplaceTypeReference(source, typeName, simpleName);
            namespaceUsings.Add(GetOuterNamespace(typeName));
        }

        if (!blocked.Contains("Func") && !usedAliases.Contains("Func") &&
            source.Contains("global::System.Func<"))
        {
            source = source.Replace("global::System.Func<", "Func<");
            namespaceUsings.Add("System");
        }

        if (!blocked.Contains("List") && !usedAliases.Contains("List") &&
            source.Contains("global::System.Collections.Generic.List<"))
        {
            source = source.Replace("global::System.Collections.Generic.List<", "List<");
            namespaceUsings.Add("System.Collections.Generic");
        }

        source = ReplaceGenericFrameworkType(
            source, "Dictionary", "System.Collections.Generic", blocked, usedAliases, namespaceUsings);
        source = ReplaceGenericFrameworkType(
            source, "HashSet", "System.Collections.Generic", blocked, usedAliases, namespaceUsings);
        source = ReplaceGenericFrameworkType(
            source, "Collection", "System.Collections.ObjectModel", blocked, usedAliases, namespaceUsings);

        return source;
    }

    private static string ReplaceGenericFrameworkType(
        string source,
        string simpleName,
        string namespaceName,
        ISet<string> blocked,
        ISet<string> aliases,
        ISet<string> namespaceUsings)
    {
        var fullyQualifiedPrefix = $"global::{namespaceName}.{simpleName}<";
        if (blocked.Contains(simpleName) || aliases.Contains(simpleName) ||
            !source.Contains(fullyQualifiedPrefix))
            return source;

        namespaceUsings.Add(namespaceName);
        return source.Replace(fullyQualifiedPrefix, simpleName + "<");
    }

    private static string ReplaceFrameworkType(
        string source,
        string fullyQualifiedName,
        string simpleName,
        string namespaceName,
        ISet<string> blocked,
        ISet<string> aliases,
        ISet<string> namespaceUsings)
    {
        if (blocked.Contains(simpleName) || aliases.Contains(simpleName) || !source.Contains(fullyQualifiedName))
            return source;

        namespaceUsings.Add(namespaceName);
        return source.Replace(fullyQualifiedName, simpleName);
    }

    private static HashSet<string> CollectBlockedIdentifiers(CompilationUnitSyntax root)
    {
        var blocked = new HashSet<string>(StringComparer.Ordinal);
        foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            blocked.Add(declaration.Identifier.ValueText);
        foreach (var declaration in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
            blocked.Add(declaration.Identifier.ValueText);
        foreach (var declaration in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            blocked.Add(declaration.Identifier.ValueText);
        foreach (var parameter in root.DescendantNodes().OfType<ParameterSyntax>())
            blocked.Add(parameter.Identifier.ValueText);
        foreach (var variable in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            blocked.Add(variable.Identifier.ValueText);
        foreach (var usingDirective in root.Usings.Where(usingDirective => usingDirective.Alias is not null))
            blocked.Add(usingDirective.Alias!.Name.Identifier.ValueText);
        return blocked;
    }

    private static HashSet<string> CollectDeclaredTypeIdentifiers(CompilationUnitSyntax root)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);
        foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            declared.Add(declaration.Identifier.ValueText);
        foreach (var declaration in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
            declared.Add(declaration.Identifier.ValueText);
        return declared;
    }

    private static string NormalizeTypeName(string typeName)
    {
        var normalized = typeName.Trim();
        while (normalized.EndsWith("?", StringComparison.Ordinal))
            normalized = normalized.Substring(0, normalized.Length - 1);
        return normalized;
    }

    private static bool IsFrameworkType(string typeName) =>
        typeName.StartsWith("global::System.", StringComparison.Ordinal) ||
        typeName.StartsWith("global::NUnit.Framework.", StringComparison.Ordinal);

    private static string GetSimpleName(string typeName)
    {
        var syntax = ParseTypeName(typeName);
        while (syntax is NullableTypeSyntax nullable)
            syntax = nullable.ElementType;
        SimpleNameSyntax simple = syntax switch
        {
            QualifiedNameSyntax qualified => qualified.Right,
            AliasQualifiedNameSyntax alias => alias.Name,
            SimpleNameSyntax name => name,
            _ => IdentifierName("Type")
        };
        return simple.Identifier.ValueText;
    }

    private static string GetOuterNamespace(string typeName)
    {
        var withoutGlobal = typeName.Substring("global::".Length);
        var generic = withoutGlobal.IndexOf('<');
        var outerType = generic >= 0 ? withoutGlobal.Substring(0, generic) : withoutGlobal;
        var lastSeparator = outerType.LastIndexOf('.');
        return lastSeparator < 0 ? string.Empty : outerType.Substring(0, lastSeparator);
    }

    private static string CreateQualifiedAlias(
        string namespaceName,
        string simpleName,
        ISet<string> used)
    {
        var segments = namespaceName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        var prefix = string.Empty;
        for (var index = segments.Length - 1; index >= 0; index--)
        {
            prefix = SanitizeIdentifier(segments[index]) + prefix;
            var candidate = prefix + simpleName;
            if (!used.Contains(candidate))
                return candidate;
        }

        var suffix = 2;
        while (used.Contains(simpleName + suffix))
            suffix++;
        return simpleName + suffix;
    }

    private static string CreateConflictAlias(
        AliasCandidate candidate,
        IReadOnlyCollection<AliasCandidate> conflictGroup,
        ISet<string> used)
    {
        var namespaces = conflictGroup
            .Select(value => value.NamespaceName)
            .ToArray();
        var maximumDepth = namespaces
            .Select(namespaceName => SplitNamespace(namespaceName).Length)
            .DefaultIfEmpty(0)
            .Max();

        for (var depth = 1; depth <= maximumDepth; depth++)
        {
            var groupAliases = conflictGroup
                .Select(value => CreateNamespaceSuffixAlias(
                    value.NamespaceName,
                    value.SimpleName,
                    depth))
                .ToArray();
            if (groupAliases.Distinct(StringComparer.Ordinal).Count() != groupAliases.Length)
                continue;

            var alias = CreateNamespaceSuffixAlias(
                candidate.NamespaceName,
                candidate.SimpleName,
                depth);
            if (!used.Contains(alias))
                return alias;
        }

        return CreateQualifiedAlias(
            candidate.NamespaceName,
            candidate.SimpleName,
            used);
    }

    private static string CreateNamespaceSuffixAlias(
        string namespaceName,
        string simpleName,
        int depth)
    {
        var segments = SplitNamespace(namespaceName);
        var first = Math.Max(0, segments.Length - depth);
        return string.Concat(segments.Skip(first).Select(SanitizeIdentifier)) + simpleName;
    }

    private static string[] SplitNamespace(string namespaceName) =>
        namespaceName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

    private static string ReplaceTypeReference(
        string source,
        string fullyQualifiedName,
        string replacement) =>
        Regex.Replace(
            source,
            Regex.Escape(fullyQualifiedName) + "(?![A-Za-z0-9_])",
            replacement,
            RegexOptions.CultureInvariant);

    private static string SanitizeIdentifier(string value)
    {
        var characters = value.Where(SyntaxFacts.IsIdentifierPartCharacter).ToArray();
        var result = new string(characters);
        if (result.Length == 0 || !SyntaxFacts.IsIdentifierStartCharacter(result[0]))
            result = "_" + result;
        return result;
    }

    private static string EscapeIdentifier(string identifier) =>
        SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
        SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;

    private sealed class AliasPlan
    {
        public string TypeName { get; }
        public string Replacement { get; }
        public bool RequiresUsing { get; }

        public AliasPlan(string typeName, string replacement, bool requiresUsing)
        {
            TypeName = typeName;
            Replacement = replacement;
            RequiresUsing = requiresUsing;
        }
    }

    private sealed class AliasCandidate
    {
        public string TypeName { get; }
        public string SimpleName { get; }
        public string NamespaceName { get; }

        public AliasCandidate(string typeName, string simpleName, string namespaceName)
        {
            TypeName = typeName;
            SimpleName = simpleName;
            NamespaceName = namespaceName;
        }
    }
}
