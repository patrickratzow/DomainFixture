using System;
using System.Collections.Generic;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.Pipeline;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Generation;

internal sealed class ValidInstancePlanningRequest
{
    public DomainTypeSpec Type { get; }
    public DomainRecipeSpec? Recipe { get; }
    public Func<string, NestedValidInstanceResolution> NestedResolver { get; }
    public IReadOnlyList<ConfiguredValueSpec> ConfiguredValues { get; }
    public IReadOnlyList<InferredValueSpec> InferredValues { get; }

    public ValidInstancePlanningRequest(
        DomainTypeSpec type,
        DomainRecipeSpec? recipe = null,
        Func<string, NestedValidInstanceResolution>? nestedResolver = null,
        IReadOnlyList<ConfiguredValueSpec>? configuredValues = null,
        IReadOnlyList<InferredValueSpec>? inferredValues = null)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Recipe = recipe;
        NestedResolver = nestedResolver ?? (typeName =>
            NestedValidInstanceResolution.Uncovered(
                $"no nested recipe factory is available for '{typeName}'"));
        ConfiguredValues = configuredValues ?? new ConfiguredValueSpec[0];
        InferredValues = inferredValues is not null
            ? inferredValues
            : recipe is not null
                ? recipe.Configuration.InferredValues
                : type.Recipes
                    .SelectMany(candidate => candidate.Configuration.InferredValues)
                    .ToArray();
    }
}

internal static class ValidInstanceProviderPipeline
{
    private static readonly ProviderPipeline<
        ValidInstancePlanningRequest,
        ValidInstancePlanningResult> Pipeline = new(
        new IPipelineProvider<ValidInstancePlanningRequest, ValidInstancePlanningResult>[]
        {
            new RecipeBaselineValidInstanceProvider(),
            new ConstructionSynthesisValidInstanceProvider()
        },
        ProviderPipelineMode.FirstHandled);

    public static ValidInstancePlanningResult Resolve(
        ValidInstancePlanningRequest request)
    {
        var resolution = Pipeline.Resolve(request);
        return resolution.Kind switch
        {
            ProviderResolutionKind.Handled => resolution.Output!,
            ProviderResolutionKind.Invalid => ValidInstancePlanningResult.Uncovered(
                $"valid-instance provider '{resolution.ProviderId}' failed: {resolution.Reason}"),
            ProviderResolutionKind.Unhandled => ValidInstancePlanningResult.Uncovered(
                $"type '{request.Type.SubjectTypeName}' has no recipe baseline or supported construction operation"),
            _ => ValidInstancePlanningResult.Uncovered(
                $"type '{request.Type.SubjectTypeName}' matched multiple valid-instance providers")
        };
    }
}

internal sealed class RecipeBaselineValidInstanceProvider :
    IPipelineProvider<ValidInstancePlanningRequest, ValidInstancePlanningResult>
{
    public string Id => "domainfixture.valid-instances.recipe-baseline";

    public ProviderDecision<ValidInstancePlanningResult> Evaluate(
        ValidInstancePlanningRequest request)
    {
        var recipe = request.Recipe ?? request.Type.Recipes.FirstOrDefault();
        if (recipe is null)
            return ProviderDecision<ValidInstancePlanningResult>.NotHandled();
        if (recipe.Configuration.UsesSynthesizedBaseline)
            return ProviderDecision<ValidInstancePlanningResult>.NotHandled();
        if (recipe.Configuration.SubjectTypeName != request.Type.SubjectTypeName)
        {
            return ProviderDecision<ValidInstancePlanningResult>.Invalid(
                $"recipe '{recipe.SourceId}' targets '{recipe.Configuration.SubjectTypeName}', not '{request.Type.SubjectTypeName}'");
        }

        var baseline = recipe.Configuration.BaselineFactoryExpression;
        if (string.IsNullOrWhiteSpace(baseline))
            return ProviderDecision<ValidInstancePlanningResult>.NotHandled();

        return ProviderDecision<ValidInstancePlanningResult>.Handled(
            ValidInstancePlanningResult.Covered(
                new ValidInstancePlan(
                    request.Type.SubjectTypeName,
                    baseline,
                    null,
                    null,
                    ValidInstanceProvenance.RecipeBaseline,
                    recipe.SourceId)));
    }
}

internal sealed class ConstructionSynthesisValidInstanceProvider :
    IPipelineProvider<ValidInstancePlanningRequest, ValidInstancePlanningResult>
{
    public string Id => "domainfixture.valid-instances.construction-synthesis";

    public ProviderDecision<ValidInstancePlanningResult> Evaluate(
        ValidInstancePlanningRequest request)
    {
        if (request.Type.ConstructionOperations.Length == 0)
            return ProviderDecision<ValidInstancePlanningResult>.NotHandled();

        var uncovered = new List<string>();
        foreach (var operationFact in request.Type.ConstructionOperations)
        {
            var operation = operationFact.Value;
            if (operation.KindId is not DomainOperationKinds.Constructor and not DomainOperationKinds.StaticFactory)
            {
                uncovered.Add(
                    $"operation '{operation.OperationId}' has unsupported construction kind '{operation.KindId}'");
                continue;
            }

            var parameters = new List<ValidInstanceParameterPlan>();
            var operationCovered = true;
            foreach (var parameter in operation.Parameters)
            {
                var constraints = request.Type.Constraints
                    .Select(fact => fact.Value)
                    .Where(constraint =>
                        constraint.MemberPath == parameter.MemberPath)
                    .ToArray();
                var value = ValidInstanceValueProviderPipeline.Resolve(
                    new ValidInstanceValuePlanningRequest(
                        parameter,
                        constraints,
                        request.NestedResolver,
                        request.ConfiguredValues,
                        request.InferredValues));
                if (!value.IsCovered)
                {
                    uncovered.Add(
                        $"operation '{operation.OperationId}' parameter '{parameter.Name}' is uncovered: {value.UncoveredReason}");
                    operationCovered = false;
                    break;
                }

                parameters.Add(new ValidInstanceParameterPlan(
                    parameter,
                    value.Plan!.Expression,
                    value.Plan.Provenance));
            }

            if (!operationCovered)
                continue;

            var arguments = string.Join(", ", parameters.Select(parameter => parameter.Expression));
            var invocation = operation.KindId == DomainOperationKinds.Constructor
                ? $"new {operation.DeclaringTypeName}({arguments})"
                : $"{operation.DeclaringTypeName}.{operation.MemberName}({arguments})";
            var expression = invocation;
            if (operation.ReturnTypeName != operation.SubjectTypeName)
            {
                var resultOutcome = request.Type.Outcomes
                    .Select(outcome => outcome.Value)
                    .SingleOrDefault(outcome =>
                        outcome.OperationId == operation.OperationId &&
                        outcome.KindId == DomainOperationOutcomeKinds.ReturnsResult);
                if (resultOutcome is null)
                {
                    uncovered.Add(
                        $"operation '{operation.OperationId}' returns '{operation.ReturnTypeName}' without a configured result outcome");
                    continue;
                }

                expression =
                    $"new global::System.Func<{operation.SubjectTypeName}>(() => {{ " +
                    $"var result = {invocation}; " +
                    $"if (!result.{resultOutcome.Parameters[0]}) throw new global::System.InvalidOperationException(\"The configured result factory did not produce a successful value.\"); " +
                    $"return result.{resultOutcome.Parameters[1]}; }})()";
            }
            return ProviderDecision<ValidInstancePlanningResult>.Handled(
                ValidInstancePlanningResult.Covered(
                    new ValidInstancePlan(
                        request.Type.SubjectTypeName,
                        expression,
                        operation,
                        parameters,
                        ValidInstanceProvenance.ConstructionOperation,
                        operationFact.SourceId,
                        operationFact.Provenance)));
        }

        return ProviderDecision<ValidInstancePlanningResult>.Handled(
            ValidInstancePlanningResult.Uncovered(uncovered.Count > 0
                ? uncovered
                : new[]
                {
                    $"type '{request.Type.SubjectTypeName}' has no supported constructor or static factory"
                }));
    }
}
