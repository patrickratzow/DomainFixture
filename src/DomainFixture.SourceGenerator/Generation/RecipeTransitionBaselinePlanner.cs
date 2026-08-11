using System;
using System.Collections.Generic;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Generation;

internal sealed class RecipeTransitionBaselinePlanningResult
{
    public string? Expression { get; }
    public string? FailureReason { get; }
    public bool IsCovered => Expression is not null;

    private RecipeTransitionBaselinePlanningResult(
        string? expression,
        string? failureReason)
    {
        Expression = expression;
        FailureReason = failureReason;
    }

    public static RecipeTransitionBaselinePlanningResult Covered(string expression) =>
        new(expression, null);

    public static RecipeTransitionBaselinePlanningResult Uncovered(string reason) =>
        new(null, reason);
}

internal static class RecipeTransitionBaselinePlanner
{
    public static RecipeTransitionBaselinePlanningResult Resolve(
        string subjectTypeName,
        string sourceFactoryExpression,
        DomainTransitionSpec transition,
        IReadOnlyList<DomainConstraintContract> constraints,
        Func<string, NestedValidInstanceResolution> nestedResolver,
        IReadOnlyList<ConfiguredValueSpec> configuredValues,
        IReadOnlyList<InferredValueSpec> inferredValues)
    {
        if (transition.IsRejection)
        {
            return RecipeTransitionBaselinePlanningResult.Uncovered(
                $"transition '{transition.Name}' is a rejection and cannot produce a valid state");
        }

        if (transition.ExecutionKind == DomainTransitionExecutionKind.ResultCommand)
        {
            return RecipeTransitionBaselinePlanningResult.Uncovered(
                $"result transition '{transition.Name}' does not return the subject and cannot produce a valid state");
        }

        if (!DomainTransitionInvocationPlanner.TryCreate(
                transition,
                constraints,
                nestedResolver,
                configuredValues,
                inferredValues,
                out var invocation,
                out var failureReason))
        {
            return RecipeTransitionBaselinePlanningResult.Uncovered(failureReason!);
        }

        var execution = transition.ExecutionKind == DomainTransitionExecutionKind.ImmutableCommand
            ? $"subject = {invocation};"
            : $"{invocation};";
        var expression =
            $"((global::System.Func<{subjectTypeName}>)(() => {{ {subjectTypeName} subject = {sourceFactoryExpression}; {execution} return subject; }}))()";
        return RecipeTransitionBaselinePlanningResult.Covered(expression);
    }
}
