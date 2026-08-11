using DomainFixture.SourceGenerator.Discovery;
using DomainFixture.SourceGenerator.Emission;
using DomainFixture.SourceGenerator.Extraction;
using DomainFixture.SourceGenerator.Generation;
using DomainFixture.SourceGenerator.Models;
using DomainFixture.SourceGenerator.Normalization;
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
            var parsedConfigurations = input.Left.Left.Left.Left;
            var profileResult = input.Left.Left.Left.Right;
            var constraints = input.Left.Left.Right;
            var scenarios = input.Left.Right;
            var manifests = input.Right;
            var configurations = ApplyRecipeSynthesisDefaults(
                productionContext,
                parsedConfigurations,
                profileResult.Profile);
            var discovered = DomainSpecNormalizer.Normalize(
                configurations,
                profileResult.Profile,
                constraints,
                scenarios,
                manifests);
            var baselineResolution = RecipeBaselineResolver.Resolve(
                discovered,
                profileResult.Profile);
            foreach (var failure in baselineResolution.Failures)
            {
                productionContext.ReportDiagnostic(
                    Diagnostics.GeneratorDiagnostics.ValidInstancePlanMissing(
                        failure.Recipe.Configuration.TransitionSource?.Location ??
                        failure.Recipe.Configuration.Location,
                        failure.Recipe.Name,
                        failure.Type.SubjectTypeName,
                        string.Join("; ", failure.Reasons)));
            }
            var resolvedConfigurations = baselineResolution.Configurations;
            if (!resolvedConfigurations.IsEmpty)
                UniqueValueSourceEmitter.Emit(productionContext);
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

    private static ImmutableArray<FixtureGenerationSpec> ApplyRecipeSynthesisDefaults(
        SourceProductionContext context,
        ImmutableArray<FixtureGenerationSpec> configurations,
        GenerationProfileSpec profile)
    {
        var prepared = ImmutableArray.CreateBuilder<FixtureGenerationSpec>();
        foreach (var configuration in configurations)
        {
            if (configuration.HasBaselineSource)
            {
                prepared.Add(configuration);
                continue;
            }

            if (profile.AutoSynthesizeRecipes)
            {
                prepared.Add(configuration.WithSynthesizedBaseline());
                continue;
            }

            context.ReportDiagnostic(
                Diagnostics.GeneratorDiagnostics.IncompleteConfiguration(
                    configuration.Location,
                    configuration.ConfigurationName));
        }

        return prepared.ToImmutable();
    }

    private static FixtureGenerationSpec CreateTestBaselineReference(
        FixtureGenerationSpec configuration)
    {
        if (!configuration.UsesGeneratedBaseline)
            return configuration;

        var recipeIdentifier = FixtureFactorySourceEmitter.CreateRecipeIdentifier(
            configuration.RecipeName);
        return configuration.WithBaselineFactoryExpression(
            $"global::{configuration.NamespaceName}.{configuration.ConfigurationName}Factory.{recipeIdentifier}.Create()");
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
