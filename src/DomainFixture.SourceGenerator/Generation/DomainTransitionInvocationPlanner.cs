using System;
using System.Collections.Generic;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis.CSharp;

namespace DomainFixture.SourceGenerator.Generation;

internal static class DomainTransitionInvocationPlanner
{
    public static bool TryCreate(
        DomainTransitionSpec transition,
        IReadOnlyList<DomainConstraintContract> constraints,
        Func<string, NestedValidInstanceResolution> nestedResolver,
        IReadOnlyList<ConfiguredValueSpec> configuredValues,
        IReadOnlyList<InferredValueSpec> inferredValues,
        out string? invocation,
        out string? failureReason,
        string subjectExpression = "subject")
    {
        var arguments = new List<string>();
        foreach (var argument in transition.Arguments.OrderBy(candidate => candidate.Position))
        {
            if (argument.Source == DomainCommandArgumentSource.ExplicitExpression)
            {
                arguments.Add(argument.ExpressionText);
                continue;
            }

            var parameter = new DomainOperationParameterContract(
                argument.ParameterName,
                argument.TypeName,
                argument.ParameterName);
            var value = ValidInstanceValueProviderPipeline.Resolve(
                new ValidInstanceValuePlanningRequest(
                    parameter,
                    constraints.Where(constraint =>
                        constraint.MemberPath == argument.ParameterName).ToArray(),
                    nestedResolver,
                    configuredValues,
                    inferredValues));
            if (!value.IsCovered)
            {
                invocation = null;
                failureReason =
                    $"command argument '{argument.ParameterName}' is uncovered: {value.UncoveredReason}";
                return false;
            }

            arguments.Add(value.Plan!.Expression);
        }

        invocation =
            $"{subjectExpression}.{EscapeIdentifier(transition.Operation.MemberName)}({string.Join(", ", arguments)})";
        failureReason = null;
        return true;
    }

    private static string EscapeIdentifier(string identifier) =>
        SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
        SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;
}
