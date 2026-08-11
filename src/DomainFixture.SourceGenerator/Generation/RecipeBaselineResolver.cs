using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.SourceGenerator.Emission;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Generation;

internal sealed class RecipeBaselineResolutionFailure
{
    public DomainRecipeSpec Recipe { get; }
    public DomainTypeSpec Type { get; }
    public ImmutableArray<string> Reasons { get; }

    public RecipeBaselineResolutionFailure(
        DomainRecipeSpec recipe,
        DomainTypeSpec type,
        IEnumerable<string> reasons)
    {
        Recipe = recipe;
        Type = type;
        Reasons = reasons.ToImmutableArray();
    }
}

internal sealed class RecipeBaselineResolution
{
    public ImmutableArray<FixtureGenerationSpec> Configurations { get; }
    public ImmutableArray<RecipeBaselineResolutionFailure> Failures { get; }

    public RecipeBaselineResolution(
        ImmutableArray<FixtureGenerationSpec> configurations,
        ImmutableArray<RecipeBaselineResolutionFailure> failures)
    {
        Configurations = configurations;
        Failures = failures;
    }
}

internal static class RecipeBaselineResolver
{
    public static RecipeBaselineResolution Resolve(
        DomainSpecNormalizationResult normalization,
        GenerationProfileSpec profile)
    {
        var recipes = normalization.Types
            .SelectMany(type => type.Recipes.Select(recipe => new RecipeEntry(type, recipe)))
            .ToArray();
        var resolved = new Dictionary<string, FixtureGenerationSpec>(
            System.StringComparer.Ordinal);
        var failures = ImmutableArray.CreateBuilder<RecipeBaselineResolutionFailure>();
        var inferredValues = recipes
            .SelectMany(entry => entry.Recipe.Configuration.InferredValues)
            .GroupBy(value => new { value.TypeName, value.Expression })
            .Select(group => group.First())
            .ToArray();

        foreach (var entry in recipes.Where(entry =>
                     entry.Recipe.Configuration.TransitionSource is null))
        {
            var result = ResolveValidInstancePlan(
                normalization,
                entry.Type,
                entry.Recipe,
                new HashSet<string>(System.StringComparer.Ordinal),
                profile,
                inferredValues);
            if (!result.IsCovered)
            {
                failures.Add(new RecipeBaselineResolutionFailure(
                    entry.Recipe,
                    entry.Type,
                    result.UncoveredReasons));
                continue;
            }

            resolved[CreateRecipeKey(entry.Recipe.Configuration)] =
                entry.Recipe.Configuration.WithBaselineFactoryExpression(result.Plan!.Expression);
        }

        ResolveTransitionRecipes(
            normalization,
            profile,
            recipes,
            resolved,
            failures,
            inferredValues);

        var configurations = normalization.Types
            .SelectMany(type => type.Recipes)
            .Select(recipe => resolved.TryGetValue(
                CreateRecipeKey(recipe.Configuration),
                out var configuration)
                    ? configuration
                    : null)
            .Where(configuration => configuration is not null)
            .Cast<FixtureGenerationSpec>()
            .ToImmutableArray();

        return new RecipeBaselineResolution(
            configurations,
            failures.ToImmutable());
    }

    private static void ResolveTransitionRecipes(
        DomainSpecNormalizationResult normalization,
        GenerationProfileSpec profile,
        IReadOnlyCollection<RecipeEntry> recipes,
        IDictionary<string, FixtureGenerationSpec> resolved,
        ImmutableArray<RecipeBaselineResolutionFailure>.Builder failures,
        IReadOnlyList<InferredValueSpec> inferredValues)
    {
        var pending = recipes
            .Where(entry => entry.Recipe.Configuration.TransitionSource is not null)
            .ToList();
        while (pending.Count > 0)
        {
            var progress = false;
            for (var index = pending.Count - 1; index >= 0; index--)
            {
                var entry = pending[index];
                var source = entry.Recipe.Configuration.TransitionSource!;
                var sourceMatches = entry.Type.Recipes
                    .Where(candidate =>
                        candidate.Configuration.ConfigurationName ==
                        entry.Recipe.Configuration.ConfigurationName &&
                        candidate.Name == source.RecipeName)
                    .ToArray();
                if (sourceMatches.Length != 1)
                {
                    failures.Add(Failure(
                        entry,
                        sourceMatches.Length == 0
                            ? $"source recipe '{source.RecipeName}' was not found in fixture '{entry.Recipe.Configuration.ConfigurationName}'"
                            : $"source recipe '{source.RecipeName}' is ambiguous in fixture '{entry.Recipe.Configuration.ConfigurationName}'"));
                    pending.RemoveAt(index);
                    progress = true;
                    continue;
                }

                var sourceRecipe = sourceMatches[0];
                if (!resolved.ContainsKey(CreateRecipeKey(sourceRecipe.Configuration)))
                    continue;

                var transitions = sourceRecipe.Configuration.Transitions
                    .Where(candidate =>
                        !candidate.IsRejection &&
                        candidate.Name == source.TransitionName)
                    .ToArray();
                if (transitions.Length != 1)
                {
                    failures.Add(Failure(
                        entry,
                        transitions.Length == 0
                            ? $"successful transition '{source.TransitionName}' was not found on recipe '{source.RecipeName}'"
                            : $"successful transition '{source.TransitionName}' is ambiguous on recipe '{source.RecipeName}'"));
                    pending.RemoveAt(index);
                    progress = true;
                    continue;
                }

                var sourceConfiguration = sourceRecipe.Configuration;
                var sourceFactoryExpression = CreateFactoryExpression(
                    sourceConfiguration,
                    sourceRecipe.Name);
                var plan = RecipeTransitionBaselinePlanner.Resolve(
                    entry.Type.SubjectTypeName,
                    sourceFactoryExpression,
                    transitions[0],
                    entry.Type.Constraints.Select(fact => fact.Value).ToArray(),
                    nestedTypeName => ResolveNestedValidInstance(
                        normalization,
                        nestedTypeName,
                        entry.Type.SubjectTypeName,
                        profile,
                        inferredValues),
                    profile.ConfiguredValues,
                    inferredValues);
                if (!plan.IsCovered)
                {
                    failures.Add(Failure(entry, plan.FailureReason!));
                    pending.RemoveAt(index);
                    progress = true;
                    continue;
                }

                resolved[CreateRecipeKey(entry.Recipe.Configuration)] =
                    entry.Recipe.Configuration.WithBaselineFactoryExpression(plan.Expression!);
                pending.RemoveAt(index);
                progress = true;
            }

            if (progress)
                continue;

            foreach (var entry in pending)
            {
                failures.Add(Failure(
                    entry,
                    "recipe-transition dependency is cyclic or its source recipe could not be generated"));
            }
            break;
        }
    }

    private static RecipeBaselineResolutionFailure Failure(
        RecipeEntry entry,
        string reason) =>
        new(entry.Recipe, entry.Type, new[] { reason });

    private static string CreateRecipeKey(FixtureGenerationSpec configuration) =>
        $"{configuration.NamespaceName}|{configuration.ConfigurationName}|{configuration.SubjectTypeName}|{configuration.RecipeName}";

    private static string CreateFactoryExpression(
        FixtureGenerationSpec configuration,
        string recipeName)
    {
        var identifier = FixtureFactorySourceEmitter.CreateRecipeIdentifier(recipeName);
        return
            $"global::{configuration.NamespaceName}.{configuration.ConfigurationName}Factory.{identifier}.Create()";
    }

    private static NestedValidInstanceResolution ResolveNestedValidInstance(
        DomainSpecNormalizationResult normalization,
        string nestedTypeName,
        string parentTypeName,
        GenerationProfileSpec profile,
        IReadOnlyList<InferredValueSpec> inferredValues)
    {
        var nestedType = normalization.Types.FirstOrDefault(candidate =>
            candidate.SubjectTypeName == nestedTypeName);
        if (nestedType is null)
        {
            return NestedValidInstanceResolution.Uncovered(
                $"no normalized domain type exists for '{nestedTypeName}'");
        }

        var nestedRecipe = nestedType.Recipes.FirstOrDefault(candidate =>
            candidate.Configuration.TransitionSource is null);
        var nestedResult = ResolveValidInstancePlan(
            normalization,
            nestedType,
            nestedRecipe,
            new HashSet<string>(System.StringComparer.Ordinal) { parentTypeName },
            profile,
            inferredValues);
        if (!nestedResult.IsCovered)
        {
            return NestedValidInstanceResolution.Uncovered(
                string.Join("; ", nestedResult.UncoveredReasons));
        }

        return nestedRecipe is null
            ? NestedValidInstanceResolution.Covered(nestedResult.Plan!.Expression)
            : NestedValidInstanceResolution.Covered(
                CreateFactoryExpression(nestedRecipe.Configuration, nestedRecipe.Name));
    }

    private static ValidInstancePlanningResult ResolveValidInstancePlan(
        DomainSpecNormalizationResult normalization,
        DomainTypeSpec type,
        DomainRecipeSpec? recipe,
        HashSet<string> activeTypes,
        GenerationProfileSpec profile,
        IReadOnlyList<InferredValueSpec> inferredValues)
    {
        if (!activeTypes.Add(type.SubjectTypeName))
        {
            return ValidInstancePlanningResult.Uncovered(
                $"nested construction cycle detected at '{type.SubjectTypeName}'");
        }

        var result = ValidInstanceProviderPipeline.Resolve(
            new ValidInstancePlanningRequest(
                type,
                recipe,
                nestedTypeName =>
                {
                    var nestedType = normalization.Types.FirstOrDefault(candidate =>
                        candidate.SubjectTypeName == nestedTypeName);
                    if (nestedType is null)
                    {
                        return NestedValidInstanceResolution.Uncovered(
                            $"no normalized domain type exists for '{nestedTypeName}'");
                    }

                    var nestedRecipe = nestedType.Recipes.FirstOrDefault(candidate =>
                        candidate.Configuration.TransitionSource is null);
                    var nestedResult = ResolveValidInstancePlan(
                        normalization,
                        nestedType,
                        nestedRecipe,
                        new HashSet<string>(activeTypes, System.StringComparer.Ordinal),
                        profile,
                        inferredValues);
                    if (!nestedResult.IsCovered)
                    {
                        return NestedValidInstanceResolution.Uncovered(
                            string.Join("; ", nestedResult.UncoveredReasons));
                    }

                    return nestedRecipe is null
                        ? NestedValidInstanceResolution.Covered(nestedResult.Plan!.Expression)
                        : NestedValidInstanceResolution.Covered(
                            CreateFactoryExpression(
                                nestedRecipe.Configuration,
                                nestedRecipe.Name));
                },
                profile.ConfiguredValues,
                inferredValues));
        activeTypes.Remove(type.SubjectTypeName);
        return result;
    }

    private sealed class RecipeEntry
    {
        public DomainTypeSpec Type { get; }
        public DomainRecipeSpec Recipe { get; }

        public RecipeEntry(DomainTypeSpec type, DomainRecipeSpec recipe)
        {
            Type = type;
            Recipe = recipe;
        }
    }
}
