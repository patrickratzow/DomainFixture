using System.Collections.Generic;
using DomainFixture.SourceGenerator.Discovery;
using DomainFixture.SourceGenerator.Emission;
using DomainFixture.SourceGenerator.Extraction;
using DomainFixture.SourceGenerator.Generation;
using DomainFixture.SourceGenerator.Models;
using DomainFixture.SourceGenerator.Normalization;
using DomainFixture.TestGenerator.Modules;
using DomainFixture.Contracts;
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
        var moduleManifests = DomainFixtureModuleManifestProvider.Create(context);

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

        context.RegisterSourceOutput(moduleManifests, static (productionContext, catalog) =>
        {
            foreach (var issue in catalog.Issues)
            {
                var diagnostic = issue.Kind is
                    CompileTimeModuleIssueKind.UnsupportedModuleSchema or
                    CompileTimeModuleIssueKind.InvalidModule or
                    CompileTimeModuleIssueKind.ConflictingModule
                        ? Diagnostics.GeneratorDiagnostics.CompileTimeModuleInvalid(
                            location: null,
                            issue.ModuleId,
                            issue.Message)
                        : Diagnostics.GeneratorDiagnostics.CompileTimeModuleContributionInvalid(
                            location: null,
                            issue.ModuleId,
                            issue.Message);
                productionContext.ReportDiagnostic(diagnostic);
            }
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
            .Combine(operationManifests)
            .Combine(moduleManifests)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(allGenerationInputs, static (productionContext, input) =>
        {
            var generationInput = input.Left;
            var compilation = input.Right;
            var domainInput = generationInput.Left;
            var moduleCatalog = generationInput.Right;
            CompileTimeModuleCatalogEmitter.Emit(productionContext, moduleCatalog);
            var parsedConfigurations = domainInput.Left.Left.Left.Left;
            var profileResult = domainInput.Left.Left.Left.Right;
            var constraints = domainInput.Left.Left.Right;
            var scenarios = domainInput.Left.Right;
            var manifests = domainInput.Right;
            var reportedModuleCapabilities = new HashSet<string>(System.StringComparer.Ordinal);
            constraints = constraints.Where(constraint => IsModuleContributionAllowed(
                    productionContext,
                    moduleCatalog,
                    constraint.Contract.SourceTypeName,
                    DomainFixtureModuleCapabilities.Constraints,
                    reportedModuleCapabilities))
                .ToImmutableArray();
            scenarios = scenarios.Where(scenario => IsModuleContributionAllowed(
                    productionContext,
                    moduleCatalog,
                    scenario.Contract.SourceTypeName,
                    DomainFixtureModuleCapabilities.Scenarios,
                    reportedModuleCapabilities))
                .ToImmutableArray();
            manifests = new DomainOperationManifestExtraction(
                manifests.Operations.Where(operation => IsModuleContributionAllowed(
                        productionContext,
                        moduleCatalog,
                        operation.SourceTypeName,
                        DomainFixtureModuleCapabilities.Operations,
                        reportedModuleCapabilities))
                    .ToImmutableArray(),
                manifests.Outcomes.Where(outcome => IsModuleContributionAllowed(
                        productionContext,
                        moduleCatalog,
                        outcome.SourceTypeName,
                        DomainFixtureModuleCapabilities.OperationOutcomes,
                        reportedModuleCapabilities))
                    .ToImmutableArray(),
                manifests.Failures);
            var explicitConfigurations = ApplyRecipeSynthesisDefaults(
                productionContext,
                parsedConfigurations,
                profileResult.Profile);
            var contributedConfigurations =
                ImplicitFixtureConfigurationDiscovery.DiscoverContributedRecipes(
                    compilation,
                    explicitConfigurations,
                    moduleCatalog.Recipes);
            foreach (var diagnostic in contributedConfigurations.Diagnostics)
                productionContext.ReportDiagnostic(diagnostic);
            var rootConfigurations = explicitConfigurations.AddRange(
                contributedConfigurations.Configurations);
            var implicitConfigurations = ImplicitFixtureConfigurationDiscovery.Discover(
                compilation,
                rootConfigurations,
                profileResult.Profile);
            foreach (var diagnostic in implicitConfigurations.Diagnostics)
                productionContext.ReportDiagnostic(diagnostic);
            var configurations = ApplyModuleValidationAdapters(
                rootConfigurations.AddRange(implicitConfigurations.Configurations),
                moduleCatalog.Validations);
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

    private static ImmutableArray<FixtureGenerationSpec> ApplyModuleValidationAdapters(
        ImmutableArray<FixtureGenerationSpec> configurations,
        ImmutableArray<CompileTimeValidationContribution> validations)
    {
        if (validations.IsEmpty)
            return configurations;

        return configurations.Select(configuration =>
        {
            if (configuration.ValidatorFactoryExpression is not null)
                return configuration;

            var validation = validations.FirstOrDefault(candidate =>
                candidate.SubjectTypeName == configuration.SubjectTypeName);
            return validation is null
                ? configuration
                : configuration.WithValidationAdapter(
                    $"new {validation.AdapterTypeName}()",
                    validation.SourceTypeName);
        }).ToImmutableArray();
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

    private static bool IsModuleContributionAllowed(
        SourceProductionContext context,
        CompileTimeModuleCatalog catalog,
        string sourceTypeName,
        string capability,
        ISet<string> reported)
    {
        var resolution = catalog.ResolveCapability(sourceTypeName, capability);
        if (resolution.Status is
            CompileTimeModuleCapabilityStatus.Unregistered or
            CompileTimeModuleCapabilityStatus.Supported)
        {
            return true;
        }

        var diagnosticKey = sourceTypeName + "|" + capability;
        if (reported.Add(diagnosticKey))
        {
            var moduleId = resolution.Module?.ModuleId ?? sourceTypeName;
            var reason = resolution.Status == CompileTimeModuleCapabilityStatus.MissingCapability
                ? $"source '{sourceTypeName}' contributed '{capability}', but module '{moduleId}' did not declare that capability"
                : $"source '{sourceTypeName}' is claimed by multiple compile-time modules";
            context.ReportDiagnostic(
                Diagnostics.GeneratorDiagnostics.CompileTimeModuleContributionInvalid(
                    location: null,
                    moduleId,
                    reason));
        }

        return false;
    }
}
