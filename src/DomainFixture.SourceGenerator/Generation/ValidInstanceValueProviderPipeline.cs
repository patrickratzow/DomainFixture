using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.Pipeline;
using DomainFixture.SourceGenerator.Emission;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis.CSharp;

namespace DomainFixture.SourceGenerator.Generation;

internal sealed class ValidInstanceValuePlanningRequest
{
    public DomainOperationParameterContract Parameter { get; }
    public IReadOnlyList<DomainConstraintContract> Constraints { get; }
    public Func<string, NestedValidInstanceResolution> NestedResolver { get; }
    public IReadOnlyList<ConfiguredValueSpec> ConfiguredValues { get; }
    public IReadOnlyList<InferredValueSpec> InferredValues { get; }
    public int Depth { get; }

    public ValidInstanceValuePlanningRequest(
        DomainOperationParameterContract parameter,
        IReadOnlyList<DomainConstraintContract> constraints,
        Func<string, NestedValidInstanceResolution>? nestedResolver = null,
        IReadOnlyList<ConfiguredValueSpec>? configuredValues = null,
        IReadOnlyList<InferredValueSpec>? inferredValues = null,
        int depth = 0)
    {
        Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        NestedResolver = nestedResolver ?? (typeName =>
            NestedValidInstanceResolution.Uncovered(
                $"no nested recipe factory is available for '{typeName}'"));
        ConfiguredValues = configuredValues ?? new ConfiguredValueSpec[0];
        InferredValues = inferredValues ?? new InferredValueSpec[0];
        Depth = depth;
    }
}

internal static class ValidInstanceValueProviderPipeline
{
    private static readonly ProviderPipeline<
        ValidInstanceValuePlanningRequest,
        ValidInstanceValuePlan> Pipeline = new(
        new IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>[]
        {
            new ConfiguredValidValueProvider(),
            new ValidStringValueProvider(),
            new ValidInt32ValueProvider(),
            new ValidDecimalValueProvider(),
            new ValidBooleanValueProvider(),
            new ValidGuidValueProvider(),
            new ValidDictionaryValueProvider(),
            new ValidCollectionValueProvider(),
            new ValidNestedValueProvider(),
            new InferredValidValueProvider()
        },
        ProviderPipelineMode.FirstHandled);

    public static ValidInstanceValuePlanningResult Resolve(
        ValidInstanceValuePlanningRequest request)
    {
        var resolution = Pipeline.Resolve(request);
        return resolution.Kind switch
        {
            ProviderResolutionKind.Handled =>
                ValidInstanceValuePlanningResult.Covered(
                    resolution.Output!,
                    resolution.ProviderId!),
            ProviderResolutionKind.Invalid =>
                ValidInstanceValuePlanningResult.Uncovered(
                    resolution.Reason!,
                    resolution.ProviderId),
            ProviderResolutionKind.Unhandled =>
                ValidInstanceValuePlanningResult.Uncovered(
                    $"no valid-value provider supports parameter '{request.Parameter.Name}' of type '{request.Parameter.TypeName}'"),
            _ => ValidInstanceValuePlanningResult.Uncovered(
                $"more than one valid-value provider matched type '{request.Parameter.TypeName}'")
        };
    }
}

internal sealed class ConfiguredValidValueProvider :
    IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>
{
    public string Id => "domainfixture.valid-values.configured";

    public ProviderDecision<ValidInstanceValuePlan> Evaluate(
        ValidInstanceValuePlanningRequest request)
    {
        var typeName = ValidInstanceTypeNames.NormalizeLookup(request.Parameter.TypeName);
        var matches = request.ConfiguredValues
            .Where(value => ValidInstanceTypeNames.NormalizeLookup(value.TypeName) == typeName)
            .ToArray();
        if (matches.Length == 0)
            return ProviderDecision<ValidInstanceValuePlan>.NotHandled();
        if (matches.Length > 1)
            return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                $"type '{typeName}' has more than one configured value");

        return ProviderDecision<ValidInstanceValuePlan>.Handled(
            new ValidInstanceValuePlan(
                request.Parameter.TypeName,
                matches[0].Expression,
                ValidInstanceProvenance.ConfiguredValue));
    }
}

internal sealed class ValidStringValueProvider :
    IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>
{
    public string Id => "domainfixture.valid-values.string";

    public ProviderDecision<ValidInstanceValuePlan> Evaluate(
        ValidInstanceValuePlanningRequest request)
    {
        if (!ValidInstanceTypeNames.IsString(request.Parameter.TypeName))
            return ProviderDecision<ValidInstanceValuePlan>.NotHandled();

        var minimum = 0;
        int? maximum = null;
        var hasConstraint = false;
        foreach (var constraint in request.Constraints)
        {
            switch (constraint.KindId)
            {
                case DomainConstraintKinds.TextLength:
                    if (!constraint.TryGetInt32(DomainConstraintParameters.Minimum, out var lengthMinimum) ||
                        !constraint.TryGetInt32(DomainConstraintParameters.Maximum, out var lengthMaximum))
                    {
                        return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                            "a text-length constraint is missing its Int32 minimum or maximum");
                    }

                    minimum = Math.Max(minimum, lengthMinimum);
                    maximum = Min(maximum, lengthMaximum);
                    hasConstraint = true;
                    break;
                case DomainConstraintKinds.TextMinimumLength:
                    if (!constraint.TryGetInt32(DomainConstraintParameters.Minimum, out var parsedMinimum))
                    {
                        return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                            "a minimum-length constraint is missing its Int32 minimum");
                    }

                    minimum = Math.Max(minimum, parsedMinimum);
                    hasConstraint = true;
                    break;
                case DomainConstraintKinds.TextMaximumLength:
                    if (!constraint.TryGetInt32(DomainConstraintParameters.Maximum, out var parsedMaximum))
                    {
                        return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                            "a maximum-length constraint is missing its Int32 maximum");
                    }

                    maximum = Min(maximum, parsedMaximum);
                    hasConstraint = true;
                    break;
                case DomainConstraintKinds.TextNotEmpty:
                    minimum = Math.Max(minimum, 1);
                    hasConstraint = true;
                    break;
                case DomainConstraintKinds.TextNotNull:
                    hasConstraint = true;
                    break;
            }
        }

        if (minimum < 0 || maximum is < 0 || maximum is not null && minimum > maximum)
        {
            return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                $"string constraints have no valid length interval ({minimum}..{maximum?.ToString(CultureInfo.InvariantCulture) ?? "unbounded"})");
        }

        var length = Math.Max(minimum, 1);
        if (maximum is not null)
            length = Math.Min(length, maximum.Value);
        var expression =
            $"{UniqueValueSourceEmitter.StringFactoryExpression}({length}, {maximum?.ToString(CultureInfo.InvariantCulture) ?? "2147483647"})";
        return ProviderDecision<ValidInstanceValuePlan>.Handled(
            new ValidInstanceValuePlan(
                request.Parameter.TypeName,
                expression,
                hasConstraint
                    ? ValidInstanceProvenance.StringConstraint
                    : ValidInstanceProvenance.PrimitiveDefault));
    }

    private static int? Min(int? current, int value) =>
        current is null ? value : Math.Min(current.Value, value);
}

internal sealed class ValidInt32ValueProvider :
    IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>
{
    public string Id => "domainfixture.valid-values.int32";

    public ProviderDecision<ValidInstanceValuePlan> Evaluate(
        ValidInstanceValuePlanningRequest request)
    {
        if (!ValidInstanceTypeNames.IsInt32(request.Parameter.TypeName))
            return ProviderDecision<ValidInstanceValuePlan>.NotHandled();

        long minimum = int.MinValue;
        long maximum = int.MaxValue;
        var hasConstraint = false;
        foreach (var constraint in request.Constraints)
        {
            var hasMinimum = constraint.TryGetInt32(
                DomainConstraintParameters.Minimum,
                out var parsedMinimum);
            var hasMaximum = constraint.TryGetInt32(
                DomainConstraintParameters.Maximum,
                out var parsedMaximum);
            switch (constraint.KindId)
            {
                case DomainConstraintKinds.Int32InclusiveRange when hasMinimum && hasMaximum:
                    minimum = Math.Max(minimum, parsedMinimum);
                    maximum = Math.Min(maximum, parsedMaximum);
                    hasConstraint = true;
                    break;
                case DomainConstraintKinds.Int32ExclusiveRange when hasMinimum && hasMaximum:
                    minimum = Math.Max(minimum, (long)parsedMinimum + 1);
                    maximum = Math.Min(maximum, (long)parsedMaximum - 1);
                    hasConstraint = true;
                    break;
                case DomainConstraintKinds.Int32GreaterThan when hasMinimum:
                    minimum = Math.Max(minimum, (long)parsedMinimum + 1);
                    hasConstraint = true;
                    break;
                case DomainConstraintKinds.Int32LessThan when hasMaximum:
                    maximum = Math.Min(maximum, (long)parsedMaximum - 1);
                    hasConstraint = true;
                    break;
                case DomainConstraintKinds.Int32InclusiveRange:
                case DomainConstraintKinds.Int32ExclusiveRange:
                case DomainConstraintKinds.Int32GreaterThan:
                case DomainConstraintKinds.Int32LessThan:
                    return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                        $"Int32 constraint '{constraint.KindId}' is missing a required boundary");
            }
        }

        if (minimum > maximum || minimum > int.MaxValue || maximum < int.MinValue)
        {
            return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                $"Int32 constraints have no valid interval ({minimum}..{maximum})");
        }

        var value = Math.Max(minimum, Math.Min(1, maximum));
        return ProviderDecision<ValidInstanceValuePlan>.Handled(
            new ValidInstanceValuePlan(
                request.Parameter.TypeName,
                value.ToString(CultureInfo.InvariantCulture),
                hasConstraint
                    ? ValidInstanceProvenance.Int32Constraint
                    : ValidInstanceProvenance.PrimitiveDefault));
    }
}

internal sealed class ValidDecimalValueProvider :
    IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>
{
    public string Id => "domainfixture.valid-values.decimal";

    public ProviderDecision<ValidInstanceValuePlan> Evaluate(
        ValidInstanceValuePlanningRequest request) =>
        ValidInstanceTypeNames.IsDecimal(request.Parameter.TypeName)
            ? ProviderDecision<ValidInstanceValuePlan>.Handled(
                new ValidInstanceValuePlan(
                    request.Parameter.TypeName,
                    "1M",
                    ValidInstanceProvenance.PrimitiveDefault))
            : ProviderDecision<ValidInstanceValuePlan>.NotHandled();
}

internal sealed class ValidBooleanValueProvider :
    IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>
{
    public string Id => "domainfixture.valid-values.boolean";

    public ProviderDecision<ValidInstanceValuePlan> Evaluate(
        ValidInstanceValuePlanningRequest request) =>
        ValidInstanceTypeNames.IsBoolean(request.Parameter.TypeName)
            ? ProviderDecision<ValidInstanceValuePlan>.Handled(
                new ValidInstanceValuePlan(
                    request.Parameter.TypeName,
                    "true",
                    ValidInstanceProvenance.PrimitiveDefault))
            : ProviderDecision<ValidInstanceValuePlan>.NotHandled();
}

internal sealed class ValidGuidValueProvider :
    IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>
{
    public string Id => "domainfixture.valid-values.guid";

    public ProviderDecision<ValidInstanceValuePlan> Evaluate(
        ValidInstanceValuePlanningRequest request) =>
        ValidInstanceTypeNames.IsGuid(request.Parameter.TypeName)
            ? ProviderDecision<ValidInstanceValuePlan>.Handled(
                new ValidInstanceValuePlan(
                    request.Parameter.TypeName,
                    "global::System.Guid.NewGuid()",
                    ValidInstanceProvenance.UniqueGuid))
            : ProviderDecision<ValidInstanceValuePlan>.NotHandled();
}

internal sealed class ValidCollectionValueProvider :
    IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>
{
    public string Id => "domainfixture.valid-values.collection";

    public ProviderDecision<ValidInstanceValuePlan> Evaluate(
        ValidInstanceValuePlanningRequest request)
    {
        if (!ValidInstanceTypeNames.TryGetCollectionElement(
                request.Parameter.TypeName,
                out var collectionKind,
                out var elementType))
        {
            return ProviderDecision<ValidInstanceValuePlan>.NotHandled();
        }

        if (request.Depth >= 8)
            return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                $"collection nesting exceeds the supported depth for '{request.Parameter.TypeName}'");

        var element = ValidInstanceValueProviderPipeline.Resolve(
            new ValidInstanceValuePlanningRequest(
                new DomainOperationParameterContract("item", elementType!, "item"),
                new DomainConstraintContract[0],
                request.NestedResolver,
                request.ConfiguredValues,
                request.InferredValues,
                request.Depth + 1));
        if (!element.IsCovered)
        {
            return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                $"collection element '{elementType}' is uncovered: {element.UncoveredReason}");
        }

        var expression = collectionKind switch
        {
            ValidInstanceCollectionKind.List or
            ValidInstanceCollectionKind.ListInterface or
            ValidInstanceCollectionKind.CollectionInterface =>
                $"new global::System.Collections.Generic.List<{elementType}> {{ {element.Plan!.Expression} }}",
            ValidInstanceCollectionKind.Collection =>
                $"new global::System.Collections.ObjectModel.Collection<{elementType}> {{ {element.Plan!.Expression} }}",
            ValidInstanceCollectionKind.Set or
            ValidInstanceCollectionKind.SetInterface or
            ValidInstanceCollectionKind.ReadOnlySet =>
                $"new global::System.Collections.Generic.HashSet<{elementType}> {{ {element.Plan!.Expression} }}",
            _ => $"new {elementType}[] {{ {element.Plan!.Expression} }}"
        };
        return ProviderDecision<ValidInstanceValuePlan>.Handled(
            new ValidInstanceValuePlan(
                request.Parameter.TypeName,
                expression,
                ValidInstanceProvenance.Collection));
    }
}

internal sealed class ValidDictionaryValueProvider :
    IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>
{
    public string Id => "domainfixture.valid-values.dictionary";

    public ProviderDecision<ValidInstanceValuePlan> Evaluate(
        ValidInstanceValuePlanningRequest request)
    {
        if (!ValidInstanceTypeNames.TryGetDictionaryArguments(
                request.Parameter.TypeName,
                out var keyType,
                out var valueType))
            return ProviderDecision<ValidInstanceValuePlan>.NotHandled();
        if (request.Depth >= 8)
            return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                $"dictionary nesting exceeds the supported depth for '{request.Parameter.TypeName}'");

        var key = ResolveElement(request, "key", keyType!);
        if (!key.IsCovered)
            return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                $"dictionary key '{keyType}' is uncovered: {key.UncoveredReason}");
        var value = ResolveElement(request, "value", valueType!);
        if (!value.IsCovered)
            return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                $"dictionary value '{valueType}' is uncovered: {value.UncoveredReason}");

        return ProviderDecision<ValidInstanceValuePlan>.Handled(
            new ValidInstanceValuePlan(
                request.Parameter.TypeName,
                $"new global::System.Collections.Generic.Dictionary<{keyType}, {valueType}> {{ [{key.Plan!.Expression}] = {value.Plan!.Expression} }}",
                ValidInstanceProvenance.Collection));
    }

    private static ValidInstanceValuePlanningResult ResolveElement(
        ValidInstanceValuePlanningRequest request,
        string name,
        string typeName) =>
        ValidInstanceValueProviderPipeline.Resolve(
            new ValidInstanceValuePlanningRequest(
                new DomainOperationParameterContract(name, typeName, name),
                new DomainConstraintContract[0],
                request.NestedResolver,
                request.ConfiguredValues,
                request.InferredValues,
                request.Depth + 1));
}

internal sealed class ValidNestedValueProvider :
    IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>
{
    public string Id => "domainfixture.valid-values.nested-recipe";

    public ProviderDecision<ValidInstanceValuePlan> Evaluate(
        ValidInstanceValuePlanningRequest request)
    {
        var nestedType = ValidInstanceTypeNames.UnwrapNullable(request.Parameter.TypeName);
        var nested = request.NestedResolver(nestedType);
        if (nested.IsCovered)
        {
            return ProviderDecision<ValidInstanceValuePlan>.Handled(
                new ValidInstanceValuePlan(
                    request.Parameter.TypeName,
                    nested.FactoryExpression!,
                    ValidInstanceProvenance.NestedRecipe));
        }

        var hasInferredFallback = request.InferredValues.Any(value =>
            ValidInstanceTypeNames.NormalizeLookup(value.TypeName) ==
            ValidInstanceTypeNames.NormalizeLookup(nestedType));
        return hasInferredFallback
            ? ProviderDecision<ValidInstanceValuePlan>.NotHandled()
            : ProviderDecision<ValidInstanceValuePlan>.Invalid(
                nested.FailureReason ??
                $"no nested recipe factory or inferred value is available for '{nestedType}'");
    }
}

internal sealed class InferredValidValueProvider :
    IPipelineProvider<ValidInstanceValuePlanningRequest, ValidInstanceValuePlan>
{
    public string Id => "domainfixture.valid-values.inferred";

    public ProviderDecision<ValidInstanceValuePlan> Evaluate(
        ValidInstanceValuePlanningRequest request)
    {
        var typeName = ValidInstanceTypeNames.NormalizeLookup(request.Parameter.TypeName);
        var expressions = request.InferredValues
            .Where(value =>
                ValidInstanceTypeNames.NormalizeLookup(value.TypeName) == typeName)
            .Select(value => value.Expression)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (expressions.Length == 0)
            return ProviderDecision<ValidInstanceValuePlan>.NotHandled();
        if (expressions.Length > 1)
        {
            return ProviderDecision<ValidInstanceValuePlan>.Invalid(
                $"type '{typeName}' has conflicting inferred value expressions");
        }

        return ProviderDecision<ValidInstanceValuePlan>.Handled(
            new ValidInstanceValuePlan(
                request.Parameter.TypeName,
                expressions[0],
                ValidInstanceProvenance.InferredValue));
    }
}

internal enum ValidInstanceCollectionKind
{
    Array,
    Enumerable,
    ReadOnlyCollection,
    ReadOnlyList,
    List,
    Collection,
    ListInterface,
    CollectionInterface,
    Set,
    SetInterface,
    ReadOnlySet
}

internal static class ValidInstanceTypeNames
{
    public static bool IsString(string typeName) => Matches(
        typeName,
        "string",
        "System.String",
        "global::System.String");

    public static bool IsInt32(string typeName) => Matches(
        typeName,
        "int",
        "System.Int32",
        "global::System.Int32");

    public static bool IsBoolean(string typeName) => Matches(
        typeName,
        "bool",
        "System.Boolean",
        "global::System.Boolean");

    public static bool IsDecimal(string typeName) => Matches(
        typeName,
        "decimal",
        "System.Decimal",
        "global::System.Decimal");

    public static bool IsGuid(string typeName) => Matches(
        typeName,
        "System.Guid",
        "global::System.Guid");

    public static bool TryGetCollectionElement(
        string typeName,
        out ValidInstanceCollectionKind? kind,
        out string? elementType)
    {
        var normalized = UnwrapNullable(typeName);
        if (normalized.EndsWith("[]", StringComparison.Ordinal) &&
            !normalized.EndsWith("[][]", StringComparison.Ordinal))
        {
            kind = ValidInstanceCollectionKind.Array;
            elementType = normalized.Substring(0, normalized.Length - 2);
            return elementType.Length > 0;
        }

        var prefixes = new[]
        {
            ("global::System.Collections.Generic.IEnumerable<", ValidInstanceCollectionKind.Enumerable),
            ("System.Collections.Generic.IEnumerable<", ValidInstanceCollectionKind.Enumerable),
            ("global::System.Collections.Generic.IReadOnlyCollection<", ValidInstanceCollectionKind.ReadOnlyCollection),
            ("System.Collections.Generic.IReadOnlyCollection<", ValidInstanceCollectionKind.ReadOnlyCollection),
            ("global::System.Collections.Generic.IReadOnlyList<", ValidInstanceCollectionKind.ReadOnlyList),
            ("System.Collections.Generic.IReadOnlyList<", ValidInstanceCollectionKind.ReadOnlyList),
            ("global::System.Collections.Generic.List<", ValidInstanceCollectionKind.List),
            ("System.Collections.Generic.List<", ValidInstanceCollectionKind.List),
            ("global::System.Collections.Generic.IList<", ValidInstanceCollectionKind.ListInterface),
            ("System.Collections.Generic.IList<", ValidInstanceCollectionKind.ListInterface),
            ("global::System.Collections.Generic.ICollection<", ValidInstanceCollectionKind.CollectionInterface),
            ("System.Collections.Generic.ICollection<", ValidInstanceCollectionKind.CollectionInterface),
            ("global::System.Collections.ObjectModel.Collection<", ValidInstanceCollectionKind.Collection),
            ("System.Collections.ObjectModel.Collection<", ValidInstanceCollectionKind.Collection),
            ("global::System.Collections.Generic.HashSet<", ValidInstanceCollectionKind.Set),
            ("System.Collections.Generic.HashSet<", ValidInstanceCollectionKind.Set),
            ("global::System.Collections.Generic.ISet<", ValidInstanceCollectionKind.SetInterface),
            ("System.Collections.Generic.ISet<", ValidInstanceCollectionKind.SetInterface),
            ("global::System.Collections.Generic.IReadOnlySet<", ValidInstanceCollectionKind.ReadOnlySet),
            ("System.Collections.Generic.IReadOnlySet<", ValidInstanceCollectionKind.ReadOnlySet)
        };
        foreach (var candidate in prefixes)
        {
            if (!normalized.StartsWith(candidate.Item1, StringComparison.Ordinal) ||
                !normalized.EndsWith(">", StringComparison.Ordinal))
            {
                continue;
            }

            var parsedElement = normalized.Substring(
                candidate.Item1.Length,
                normalized.Length - candidate.Item1.Length - 1);
            if (parsedElement.Length == 0 || HasTopLevelComma(parsedElement))
                break;

            kind = candidate.Item2;
            elementType = parsedElement;
            return true;
        }

        kind = null;
        elementType = null;
        return false;
    }

    public static bool TryGetDictionaryArguments(
        string typeName,
        out string? keyType,
        out string? valueType)
    {
        var normalized = UnwrapNullable(typeName);
        var prefixes = new[]
        {
            "global::System.Collections.Generic.Dictionary<",
            "System.Collections.Generic.Dictionary<",
            "global::System.Collections.Generic.IDictionary<",
            "System.Collections.Generic.IDictionary<",
            "global::System.Collections.Generic.IReadOnlyDictionary<",
            "System.Collections.Generic.IReadOnlyDictionary<"
        };
        foreach (var prefix in prefixes)
        {
            if (!normalized.StartsWith(prefix, StringComparison.Ordinal) ||
                !normalized.EndsWith(">", StringComparison.Ordinal))
                continue;

            var arguments = SplitTopLevelArguments(normalized.Substring(
                prefix.Length,
                normalized.Length - prefix.Length - 1));
            if (arguments.Length == 2)
            {
                keyType = arguments[0];
                valueType = arguments[1];
                return true;
            }
        }

        keyType = null;
        valueType = null;
        return false;
    }

    public static string UnwrapNullable(string typeName)
    {
        var normalized = RemoveWhitespace(typeName);
        if (normalized.EndsWith("?", StringComparison.Ordinal))
            return normalized.Substring(0, normalized.Length - 1);

        const string globalPrefix = "global::System.Nullable<";
        const string prefix = "System.Nullable<";
        if (normalized.StartsWith(globalPrefix, StringComparison.Ordinal) &&
            normalized.EndsWith(">", StringComparison.Ordinal))
            return normalized.Substring(globalPrefix.Length, normalized.Length - globalPrefix.Length - 1);
        if (normalized.StartsWith(prefix, StringComparison.Ordinal) &&
            normalized.EndsWith(">", StringComparison.Ordinal))
            return normalized.Substring(prefix.Length, normalized.Length - prefix.Length - 1);
        return normalized;
    }

    public static string NormalizeLookup(string typeName)
    {
        var normalized = UnwrapNullable(typeName);
        return normalized switch
        {
            "string" or "System.String" => "global::System.String",
            "int" or "System.Int32" => "global::System.Int32",
            "bool" or "System.Boolean" => "global::System.Boolean",
            "System.Guid" => "global::System.Guid",
            _ => normalized
        };
    }

    private static bool Matches(string actual, params string[] expected)
    {
        var normalized = UnwrapNullable(actual);
        return expected.Any(candidate => normalized == candidate);
    }

    private static string RemoveWhitespace(string value) =>
        new(value.Where(character => !char.IsWhiteSpace(character)).ToArray());

    private static bool HasTopLevelComma(string value)
    {
        var depth = 0;
        foreach (var character in value)
        {
            if (character == '<') depth++;
            if (character == '>') depth--;
            if (character == ',' && depth == 0) return true;
        }

        return false;
    }

    private static string[] SplitTopLevelArguments(string value)
    {
        var depth = 0;
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character == '<') depth++;
            if (character == '>') depth--;
            if (character == ',' && depth == 0)
                return new[] { value.Substring(0, index), value.Substring(index + 1) };
        }

        return new string[0];
    }
}
