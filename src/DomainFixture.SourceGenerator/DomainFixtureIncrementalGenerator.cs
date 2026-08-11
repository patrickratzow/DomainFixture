using DomainFixture.SourceGenerator.Discovery;
using DomainFixture.SourceGenerator.Emission;
using DomainFixture.SourceGenerator.Extraction;
using DomainFixture.SourceGenerator.Generation;
using DomainFixture.SourceGenerator.Models;
using DomainFixture.SourceGenerator.Normalization;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator;

[Generator]
public sealed class DomainFixtureIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configurations = FluentFixtureConfigurationProvider.Create(context);
        var generationProfile = GenerationProfileProvider.Create(context);
        var constraintProviders = DomainConstraintProviderCatalog.Create(context);
        var scenarioResults = DomainScenarioManifestProvider.Create(context);
        var operationManifests = DomainOperationManifestProvider.Create(context);

        context.RegisterSourceOutput(configurations, static (productionContext, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
                productionContext.ReportDiagnostic(diagnostic);

            foreach (var fixture in result.Configurations.GroupBy(configuration => new
                     {
                         configuration.NamespaceName,
                         configuration.ConfigurationName
                     }))
            {
                foreach (var collision in fixture
                             .GroupBy(configuration =>
                                 FixtureFactorySourceEmitter.CreateRecipeIdentifier(
                                     configuration.RecipeName))
                             .Where(group => group.Count() > 1))
                {
                    var recipes = collision.Take(2).ToArray();
                    productionContext.ReportDiagnostic(
                        Diagnostics.GeneratorDiagnostics.FixtureFactoryScenarioCollision(
                            recipes[1].Location,
                            fixture.Key.ConfigurationName,
                            recipes[0].RecipeName,
                            recipes[1].RecipeName));
                }
            }

        });

        context.RegisterSourceOutput(generationProfile, static (productionContext, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
                productionContext.ReportDiagnostic(diagnostic);
        });

        context.RegisterSourceOutput(constraintProviders.AllResults, static (productionContext, results) =>
        {
            foreach (var result in results)
            {
                if (result.Diagnostic is not null)
                    productionContext.ReportDiagnostic(result.Diagnostic);
            }
        });

        context.RegisterSourceOutput(scenarioResults, static (productionContext, results) =>
        {
            foreach (var result in results)
            {
                if (result.Diagnostic is not null)
                    productionContext.ReportDiagnostic(result.Diagnostic);
            }
        });

        context.RegisterSourceOutput(operationManifests, static (productionContext, extraction) =>
        {
            foreach (var failure in extraction.Failures)
                ReportOperationManifestFailure(productionContext, failure);
        });

        context.RegisterSourceOutput(
            constraintProviders.SourceResults,
            static (productionContext, results) =>
                ValidationRuleManifestEmitter.Emit(productionContext, results));

        var constraints = constraintProviders.AllResults.Select(static (results, _) => results
            .Where(result => result.Constraint is not null)
            .Select(result => result.Constraint!)
            .ToImmutableArray());
        var scenarios = scenarioResults.Select(static (results, _) => results
            .Where(result => result.Scenario is not null)
            .Select(result => result.Scenario!)
            .ToImmutableArray());
        var allConfigurations = configurations
            .Collect()
            .Select(static (results, _) => results
                .SelectMany(result => result.Configurations)
                .ToImmutableArray());
        var allGenerationInputs = allConfigurations
            .Combine(generationProfile)
            .Combine(constraints)
            .Combine(scenarios)
            .Combine(operationManifests);

        context.RegisterSourceOutput(allGenerationInputs, static (productionContext, input) =>
        {
            var configurations = input.Left.Left.Left.Left;
            var profileResult = input.Left.Left.Left.Right;
            var constraints = input.Left.Left.Right;
            var scenarios = input.Left.Right;
            var manifests = input.Right;
            var discovered = DomainSpecNormalizer.Normalize(
                configurations,
                profileResult.Profile,
                constraints,
                scenarios,
                manifests);
            var resolvedConfigurations = ResolveValidInstancePlans(
                productionContext,
                discovered,
                profileResult.Profile);
            var normalization = DomainSpecNormalizer.Normalize(
                resolvedConfigurations,
                profileResult.Profile,
                constraints,
                scenarios,
                manifests);

            foreach (var conflict in normalization.OperationConflicts)
            {
                productionContext.ReportDiagnostic(
                    Diagnostics.GeneratorDiagnostics.DomainOperationConflicting(
                        conflict.Candidate.Location ?? conflict.Existing.Location,
                        conflict.OperationId,
                        conflict.SubjectTypeName));
            }

            DomainSpecSnapshotEmitter.Emit(
                productionContext,
                normalization,
                profileResult.Profile);
            DomainCoverageReportEmitter.Emit(
                productionContext,
                normalization,
                profileResult.Profile);

            var configurationResult = new ConfigurationParseResult(
                resolvedConfigurations,
                ImmutableArray<Diagnostic>.Empty);
            FixtureFactorySourceEmitter.Emit(
                productionContext,
                configurationResult,
                configuration => configuration.CanExposePublicFactory);
            var testConfigurationResult = new ConfigurationParseResult(
                resolvedConfigurations.Select(CreateTestBaselineReference).ToImmutableArray(),
                ImmutableArray<Diagnostic>.Empty);
            FixtureTestSourceEmitter.Emit(
                productionContext,
                testConfigurationResult,
                profileResult,
                constraints,
                scenarios,
                manifests);
        });
    }

    private static FixtureGenerationSpec CreateTestBaselineReference(
        FixtureGenerationSpec configuration)
    {
        if (!configuration.UsesSynthesizedBaseline)
            return configuration;

        var recipeIdentifier = FixtureFactorySourceEmitter.CreateRecipeIdentifier(
            configuration.RecipeName);
        return configuration.WithBaselineFactoryExpression(
            $"global::{configuration.NamespaceName}.{configuration.ConfigurationName}Factory.{recipeIdentifier}.Create()");
    }

    private static ImmutableArray<FixtureGenerationSpec> ResolveValidInstancePlans(
        SourceProductionContext context,
        DomainSpecNormalizationResult normalization,
        GenerationProfileSpec profile)
    {
        var configurations = ImmutableArray.CreateBuilder<FixtureGenerationSpec>();
        foreach (var type in normalization.Types)
        {
            foreach (var recipe in type.Recipes)
            {
                var result = ResolveValidInstancePlan(
                    normalization,
                    type,
                    recipe,
                    new HashSet<string>(System.StringComparer.Ordinal),
                    profile);
                if (!result.IsCovered)
                {
                    context.ReportDiagnostic(
                        Diagnostics.GeneratorDiagnostics.ValidInstancePlanMissing(
                            recipe.Configuration.Location,
                            recipe.Name,
                            type.SubjectTypeName,
                            string.Join("; ", result.UncoveredReasons)));
                    continue;
                }

                configurations.Add(
                    recipe.Configuration.WithBaselineFactoryExpression(
                        result.Plan!.Expression));
            }
        }

        return configurations.ToImmutable();
    }

    private static ValidInstancePlanningResult ResolveValidInstancePlan(
        DomainSpecNormalizationResult normalization,
        DomainTypeSpec type,
        DomainRecipeSpec? recipe,
        HashSet<string> activeTypes,
        GenerationProfileSpec profile)
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

                    var nestedResult = ResolveValidInstancePlan(
                        normalization,
                        nestedType,
                        nestedType.Recipes.FirstOrDefault(),
                        new HashSet<string>(activeTypes, System.StringComparer.Ordinal),
                        profile);
                    if (!nestedResult.IsCovered)
                    {
                        return NestedValidInstanceResolution.Uncovered(
                            string.Join("; ", nestedResult.UncoveredReasons));
                    }

                    var nestedRecipe = nestedType.Recipes.FirstOrDefault();
                    if (nestedRecipe is null)
                        return NestedValidInstanceResolution.Covered(nestedResult.Plan!.Expression);
                    var recipeIdentifier = FixtureFactorySourceEmitter.CreateRecipeIdentifier(
                        nestedRecipe.Name);
                    var nestedConfiguration = nestedRecipe.Configuration;
                    return NestedValidInstanceResolution.Covered(
                        $"global::{nestedConfiguration.NamespaceName}.{nestedConfiguration.ConfigurationName}Factory.{recipeIdentifier}.Create()");
                },
                profile.ConfiguredValues));
        activeTypes.Remove(type.SubjectTypeName);
        return result;
    }

    private static void ReportOperationManifestFailure(
        SourceProductionContext context,
        DomainOperationManifestFailure failure)
    {
        var diagnostic = failure.Kind switch
        {
            DomainOperationManifestFailureKind.UnsupportedOperationSchema =>
                Diagnostics.GeneratorDiagnostics.DomainOperationSchemaUnsupported(
                    location: null,
                    failure.OperationId,
                    failure.Message),
            DomainOperationManifestFailureKind.ConflictingOperation =>
                Diagnostics.GeneratorDiagnostics.DomainOperationConflicting(
                    location: null,
                    failure.OperationId,
                    "manifested subject"),
            DomainOperationManifestFailureKind.UnsupportedOutcomeSchema =>
                Diagnostics.GeneratorDiagnostics.DomainOutcomeSchemaUnsupported(
                    location: null,
                    failure.OperationId,
                    failure.Message),
            DomainOperationManifestFailureKind.ConflictingOutcome =>
                Diagnostics.GeneratorDiagnostics.OperationRejectionConflicting(
                    location: null,
                    failure.OperationId),
            DomainOperationManifestFailureKind.MalformedOutcomeManifest or
                DomainOperationManifestFailureKind.InvalidOutcome or
                DomainOperationManifestFailureKind.OrphanOutcome =>
                Diagnostics.GeneratorDiagnostics.DomainOutcomeManifestInvalid(
                    location: null,
                    failure.OperationId,
                    failure.Message),
            _ => Diagnostics.GeneratorDiagnostics.DomainOperationManifestInvalid(
                location: null,
                failure.OperationId,
                failure.Message)
        };
        context.ReportDiagnostic(diagnostic);
    }
}
