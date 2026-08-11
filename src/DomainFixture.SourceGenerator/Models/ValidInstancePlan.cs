using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DomainFixture.Contracts;

namespace DomainFixture.SourceGenerator.Models;

internal static class ValidInstanceProvenance
{
    public const string RecipeBaseline = "recipe-baseline";
    public const string ConstructionOperation = "construction-operation";
    public const string StringConstraint = "string-constraint";
    public const string Int32Constraint = "int32-constraint";
    public const string PrimitiveDefault = "primitive-default";
    public const string DeterministicGuid = "deterministic-guid";
    public const string NestedRecipe = "nested-recipe";
    public const string Collection = "collection";
    public const string ConfiguredValue = "configured-value";
}

internal sealed class ValidInstanceValuePlan
{
    public string TypeName { get; }
    public string Expression { get; }
    public string Provenance { get; }

    public ValidInstanceValuePlan(
        string typeName,
        string expression,
        string provenance)
    {
        TypeName = typeName;
        Expression = expression;
        Provenance = provenance;
    }
}

internal sealed class ValidInstanceValuePlanningResult
{
    public ValidInstanceValuePlan? Plan { get; }
    public string? UncoveredReason { get; }
    public string? ProviderId { get; }
    public bool IsCovered => Plan is not null;

    private ValidInstanceValuePlanningResult(
        ValidInstanceValuePlan? plan,
        string? uncoveredReason,
        string? providerId)
    {
        Plan = plan;
        UncoveredReason = uncoveredReason;
        ProviderId = providerId;
    }

    public static ValidInstanceValuePlanningResult Covered(
        ValidInstanceValuePlan plan,
        string providerId) =>
        new(plan, null, providerId);

    public static ValidInstanceValuePlanningResult Uncovered(
        string reason,
        string? providerId = null) =>
        new(null, reason, providerId);
}

internal sealed class ValidInstanceParameterPlan
{
    public DomainOperationParameterContract Parameter { get; }
    public string Expression { get; }
    public string Provenance { get; }

    public ValidInstanceParameterPlan(
        DomainOperationParameterContract parameter,
        string expression,
        string provenance)
    {
        Parameter = parameter;
        Expression = expression;
        Provenance = provenance;
    }
}

internal sealed class ValidInstancePlan
{
    public string SubjectTypeName { get; }
    public string Expression { get; }
    public DomainOperationContract? ChosenOperation { get; }
    public ImmutableArray<ValidInstanceParameterPlan> Parameters { get; }
    public string Provenance { get; }
    public string? SourceId { get; }
    public DomainFactProvenance? FactProvenance { get; }

    public ValidInstancePlan(
        string subjectTypeName,
        string expression,
        DomainOperationContract? chosenOperation,
        IEnumerable<ValidInstanceParameterPlan>? parameters,
        string provenance,
        string? sourceId = null,
        DomainFactProvenance? factProvenance = null)
    {
        SubjectTypeName = subjectTypeName;
        Expression = expression;
        ChosenOperation = chosenOperation;
        Parameters = parameters?.ToImmutableArray() ??
                     ImmutableArray<ValidInstanceParameterPlan>.Empty;
        Provenance = provenance;
        SourceId = sourceId;
        FactProvenance = factProvenance;
    }
}

internal sealed class ValidInstancePlanningResult
{
    public ValidInstancePlan? Plan { get; }
    public ImmutableArray<string> UncoveredReasons { get; }
    public bool IsCovered => Plan is not null;

    private ValidInstancePlanningResult(
        ValidInstancePlan? plan,
        IEnumerable<string>? uncoveredReasons)
    {
        Plan = plan;
        UncoveredReasons = uncoveredReasons?.ToImmutableArray() ??
                           ImmutableArray<string>.Empty;
    }

    public static ValidInstancePlanningResult Covered(ValidInstancePlan plan) =>
        new(plan ?? throw new ArgumentNullException(nameof(plan)), null);

    public static ValidInstancePlanningResult Uncovered(IEnumerable<string> reasons) =>
        new(null, reasons);

    public static ValidInstancePlanningResult Uncovered(string reason) =>
        new(null, new[] { reason });
}

internal sealed class NestedValidInstanceResolution
{
    public string? FactoryExpression { get; }
    public string? FailureReason { get; }
    public bool IsCovered => FactoryExpression is not null;

    private NestedValidInstanceResolution(
        string? factoryExpression,
        string? failureReason)
    {
        FactoryExpression = factoryExpression;
        FailureReason = failureReason;
    }

    public static NestedValidInstanceResolution Covered(string factoryExpression)
    {
        if (string.IsNullOrWhiteSpace(factoryExpression))
            throw new ArgumentException("A nested factory expression is required.", nameof(factoryExpression));

        return new NestedValidInstanceResolution(factoryExpression, null);
    }

    public static NestedValidInstanceResolution Uncovered(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("An uncovered reason is required.", nameof(reason));

        return new NestedValidInstanceResolution(null, reason);
    }
}
