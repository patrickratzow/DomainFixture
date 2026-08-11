using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Discovery;
using DomainFixture.SourceGenerator.Extraction;
using DomainFixture.SourceGenerator.Generation;
using DomainFixture.SourceGenerator.Models;
using DomainFixture.Pipeline;
using DomainFixture.TestGenerator.Framework.Emitters;
using DomainFixture.TestGenerator.Generation;
using DomainFixture.TestGenerator.Model;
using DomainFixture.TestGenerator.Model.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace DomainFixture.SourceGenerator.Emission;

internal static class FixtureTestSourceEmitter
{
    public static void Emit(
        SourceProductionContext context,
        ConfigurationParseResult configurationResult,
        GenerationProfileParseResult profileResult,
        ImmutableArray<DiscoveredDomainConstraint> rules,
        ImmutableArray<DiscoveredDomainScenario> scenarios,
        DomainOperationManifestExtraction operationManifests)
    {
        if (configurationResult.Diagnostics.Any(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error) ||
            profileResult.Diagnostics.Any(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return;
        }

        foreach (var configuration in configurationResult.Configurations)
        {
            var constructionOperations = configuration.ConstructionOperations.ToList();
            var operationConflict = false;
            foreach (var manifested in operationManifests.Operations.Where(operation =>
                         operation.Contract.SubjectTypeName == configuration.SubjectTypeName &&
                         (operation.Contract.ReturnTypeName == configuration.SubjectTypeName ||
                          profileResult.Profile.ResolveOperationResult(
                              configuration.SubjectTypeName,
                              operation.Contract.ReturnTypeName) is not null ||
                          operationManifests.Outcomes.Any(outcome =>
                              outcome.SubjectTypeName == configuration.SubjectTypeName &&
                              outcome.Contract.OperationId == operation.Contract.OperationId &&
                              outcome.Contract.KindId == DomainOperationOutcomeKinds.ReturnsResult)) &&
                         operation.Contract.KindId is DomainOperationKinds.Constructor or
                             DomainOperationKinds.StaticFactory))
            {
                var existing = constructionOperations.FirstOrDefault(operation =>
                    operation.OperationId == manifested.Contract.OperationId);
                if (existing is null)
                {
                    constructionOperations.Add(manifested.Contract);
                }
                else if (!OperationContractsEqual(existing, manifested.Contract))
                {
                    context.ReportDiagnostic(GeneratorDiagnostics.DomainOperationConflicting(
                        manifested.Location ?? configuration.Location,
                        manifested.Contract.OperationId,
                        configuration.SubjectTypeName));
                    operationConflict = true;
                }
            }

            if (operationConflict)
                continue;

            var propertyMutations = profileResult.Profile.PropertyMutations
                .Where(mutation => mutation.SubjectTypeKey == configuration.SubjectTypeName)
                .ToDictionary(mutation => mutation.PropertyName);
            var matchingScenarios = scenarios
                .Where(scenario => scenario.Contract.SubjectTypeName == configuration.SubjectTypeName)
                .ToList();
            if (configuration.HasValueEqualitySemantics &&
                matchingScenarios.All(scenario =>
                    scenario.Contract.KindId != DomainScenarioKinds.ValueObjectEquality))
            {
                matchingScenarios.Add(new DiscoveredDomainScenario(
                    new DomainScenarioContract(
                        DomainScenarioKinds.ValueObjectEquality,
                        configuration.SubjectTypeName,
                        configuration.SubjectTypeName),
                    configuration.Location));
            }
            if (constructionOperations.Count > 0 &&
                matchingScenarios.All(scenario =>
                    scenario.Contract.KindId != DomainScenarioKinds.ConstructionRoundTrip))
            {
                matchingScenarios.Add(new DiscoveredDomainScenario(
                    new DomainScenarioContract(
                        DomainScenarioKinds.ConstructionRoundTrip,
                        configuration.SubjectTypeName,
                        configuration.SubjectTypeName),
                    configuration.Location));
            }

            foreach (var transition in configuration.Transitions)
            {
                matchingScenarios.Add(new DiscoveredDomainScenario(
                    DomainTransitionScenarioContractFactory.Create(configuration, transition),
                    transition.Location ?? configuration.Location));
            }

            foreach (var state in configuration.StateExpectations)
            {
                matchingScenarios.Add(new DiscoveredDomainScenario(
                    DomainTransitionScenarioContractFactory.CreateState(configuration, state),
                    state.Location ?? configuration.Location));
            }

            matchingScenarios = matchingScenarios
                .GroupBy(scenario => new
                {
                    scenario.Contract.KindId,
                    ScenarioName = scenario.Contract.Parameters.TryGetValue(
                        DomainTransitionScenarioContractFactory.ScenarioNameParameter,
                        out var scenarioName)
                        ? scenarioName
                        : null
                })
                .Select(group => group.First())
                .ToList();

            string? identityMemberPath = null;
            if (profileResult.Profile.UseEntityIdentity &&
                configuration.Transitions.Any(transition => !transition.IsRejection))
            {
                var identityCandidates = configuration.SubjectProperties
                    .Where(property =>
                        property.CanReadFromGeneratedCode && property.Name == "Id")
                    .ToArray();
                if (identityCandidates.Length == 0)
                {
                    identityCandidates = configuration.SubjectProperties
                        .Where(property =>
                            property.CanReadFromGeneratedCode &&
                            property.Name == configuration.SubjectTypeShortName + "Id")
                        .ToArray();
                }

                if (identityCandidates.Length != 1)
                {
                    context.ReportDiagnostic(GeneratorDiagnostics.TransitionIdentityMissing(
                        configuration.Location,
                        configuration.SubjectTypeName,
                        configuration.SubjectTypeShortName));
                    continue;
                }

                identityMemberPath = identityCandidates[0].Name;
            }

            var validationRulesTypeKey = configuration.ValidationRulesTypeKey;
            if (validationRulesTypeKey is null && profileResult.Profile.UseFluentValidation)
            {
                var matchingValidators = rules
                    .Where(rule => rule.SubjectTypeKey == configuration.SubjectTypeName)
                    .Select(rule => rule.SourceTypeKey)
                    .Distinct()
                    .ToArray();
                if (matchingValidators.Length == 0)
                {
                    validationRulesTypeKey = null;
                }
                else if (matchingValidators.Length > 1)
                {
                    context.ReportDiagnostic(GeneratorDiagnostics.SubjectValidatorAmbiguous(
                        configuration.Location,
                        configuration.SubjectTypeName));
                    continue;
                }
                else
                {
                    validationRulesTypeKey = matchingValidators[0];
                }
            }

            var matchingRules = validationRulesTypeKey is null
                ? new DiscoveredDomainConstraint[0]
                : rules
                    .Where(rule =>
                        rule.SourceTypeKey == validationRulesTypeKey &&
                        rule.SubjectTypeKey == configuration.SubjectTypeName)
                    .Concat(ConventionConstraintProvider.Create(
                        configuration,
                        profileResult.Profile,
                        validationRulesTypeKey))
                    .GroupBy(rule => new { rule.PropertyName, rule.KindId })
                    .Select(group => group.First())
                    .ToArray();
            if (matchingRules.Length == 0 && configuration.ValidationRulesTypeKey is not null)
            {
                context.ReportDiagnostic(GeneratorDiagnostics.MissingSupportedRules(
                    configuration.Location,
                    validationRulesTypeKey!));
                continue;
            }

            if (matchingRules.Length == 0 && matchingScenarios.Count == 0)
            {
                if (profileResult.Profile.UseFluentValidation)
                {
                    context.ReportDiagnostic(GeneratorDiagnostics.SubjectValidatorMissing(
                        configuration.Location,
                        configuration.SubjectTypeName));
                }
                else
                {
                    context.ReportDiagnostic(GeneratorDiagnostics.ValidationExecutionMissing(
                        configuration.Location,
                        configuration.ConfigurationName));
                }

                continue;
            }

            var generatedCases = new List<GeneratedValidationCase>();
            var boundaryProviderMissing = false;
            foreach (var rule in matchingRules)
            {
                var boundaryResolution = DomainBoundaryCaseProviderPipeline.Resolve(rule.Contract);
                switch (boundaryResolution.Kind)
                {
                    case ProviderResolutionKind.Handled:
                        generatedCases.AddRange(boundaryResolution.Output!);
                        break;
                    case ProviderResolutionKind.Unhandled:
                        context.ReportDiagnostic(GeneratorDiagnostics.BoundaryProviderMissing(
                            rule.Location ?? configuration.Location,
                            rule.KindId,
                            configuration.SubjectTypeName,
                            rule.PropertyName));
                        boundaryProviderMissing = true;
                        break;
                    case ProviderResolutionKind.Ambiguous:
                        context.ReportDiagnostic(GeneratorDiagnostics.BoundaryProviderAmbiguous(
                            rule.Location ?? configuration.Location,
                            rule.KindId,
                            configuration.SubjectTypeName,
                            rule.PropertyName,
                            string.Join(", ", boundaryResolution.MatchingProviderIds)));
                        boundaryProviderMissing = true;
                        break;
                    case ProviderResolutionKind.Invalid:
                        context.ReportDiagnostic(GeneratorDiagnostics.BoundaryProviderInvalid(
                            rule.Location ?? configuration.Location,
                            boundaryResolution.ProviderId!,
                            rule.KindId,
                            configuration.SubjectTypeName,
                            rule.PropertyName,
                            boundaryResolution.Reason!));
                        boundaryProviderMissing = true;
                        break;
                }
            }

            if (boundaryProviderMissing)
                continue;

            var reconstructionClassName =
                $"{configuration.SubjectTypeShortName}{configuration.RecipeName}ImmutableReconstruction";
            var validationResolutions = matchingRules
                .Select(rule => rule.PropertyName)
                .Distinct()
                .Select(propertyName => configuration.SubjectProperties.FirstOrDefault(property =>
                    property.Name == propertyName))
                .Where(property => property is not null)
                .Select(property => PropertyMutationStrategyPipeline.Resolve(
                    configuration,
                    profileResult.Profile,
                    property!,
                    propertyMutations,
                    reconstructionClassName))
                .ToArray();
            var resolutions = validationResolutions;
            var missingProperties = matchingRules
                .Where(rule => configuration.SubjectProperties.All(property =>
                    property.Name != rule.PropertyName))
                .ToArray();
            foreach (var rule in missingProperties)
            {
                context.ReportDiagnostic(GeneratorDiagnostics.PropertyMutationUnavailable(
                    rule.Location ?? configuration.Location,
                    configuration.SubjectTypeName,
                    rule.PropertyName,
                    rule.KindId,
                    "the property could not be discovered on the subject type"));
            }

            var failedResolutions = validationResolutions
                .Where(resolution => resolution.Strategy is null)
                .ToArray();
            foreach (var resolution in failedResolutions)
            {
                foreach (var rule in matchingRules.Where(rule =>
                             rule.PropertyName == resolution.Property.Name))
                {
                    context.ReportDiagnostic(GeneratorDiagnostics.PropertyMutationUnavailable(
                        rule.Location ?? configuration.Location,
                        configuration.SubjectTypeName,
                        rule.PropertyName,
                        rule.KindId,
                        resolution.FailureReason!));
                }
            }

            if (missingProperties.Length > 0 || failedResolutions.Length > 0)
                continue;

            propertyMutations = resolutions
                .Where(resolution => resolution.Mutation is not null)
                .ToDictionary(
                    resolution => resolution.Property.Name,
                    resolution => resolution.Mutation!);
            var generatedReconstruction = PropertyMutationReconstructionEmitter.Emit(
                configuration,
                reconstructionClassName,
                resolutions);

            var generatedTests = new List<GeneratedTest>();
            var generatedAdapter = string.Empty;
            if (matchingRules.Length > 0)
            {
                var cases = generatedCases
                .GroupBy(validationCase => validationCase.Name)
                .Select(group => group.First())
                .Select(validationCase => ApplyReconstruction(
                    validationCase,
                    propertyMutations))
                    .ToArray();
                var validatorFactoryExpression = configuration.ValidatorFactoryExpression;
                if (validatorFactoryExpression is null)
                {
                    if (!profileResult.Profile.UseFluentValidation)
                    {
                        context.ReportDiagnostic(GeneratorDiagnostics.ValidationExecutionMissing(
                            configuration.Location,
                            configuration.ConfigurationName));
                        continue;
                    }

                    var adapterClassName =
                        $"{configuration.SubjectTypeShortName}{configuration.RecipeName}FluentValidationAdapter";
                    validatorFactoryExpression =
                        $"new global::{configuration.NamespaceName}.{adapterClassName}()";
                    generatedAdapter = FluentValidationAdapterEmitter.Emit(
                        configuration.NamespaceName,
                        adapterClassName,
                        configuration.SubjectTypeName,
                        validationRulesTypeKey!);
                }

                var descriptor = new ValidationTestSuiteDescriptor(
                    configuration.NamespaceName,
                    $"{configuration.SubjectTypeShortName}{configuration.RecipeName}GeneratedTests",
                    configuration.RecipeName,
                    SyntaxFactory.ParseTypeName(configuration.SubjectTypeName),
                    SyntaxFactory.ParseExpression(configuration.BaselineFactoryExpression),
                    SyntaxFactory.ParseExpression(validatorFactoryExpression),
                    cases);
                generatedTests.AddRange(new ValidationTestSuiteBuilder().Build(descriptor).Tests);
            }

            var profileRejection = profileResult.Profile.ResolveOperationRejection(
                configuration.SubjectTypeName);
            if (profileRejection is not null && constructionOperations.Count == 0)
            {
                context.ReportDiagnostic(GeneratorDiagnostics.ConfiguredOperationMissing(
                    profileRejection.Location ?? configuration.Location,
                    configuration.SubjectTypeName));
                continue;
            }

            foreach (var operation in constructionOperations)
            {
                var manifestedOutcome = operationManifests.Outcomes.FirstOrDefault(outcome =>
                    outcome.SubjectTypeName == configuration.SubjectTypeName &&
                    outcome.Contract.OperationId == operation.OperationId);
                DomainOperationOutcomeContract? outcomeContract = manifestedOutcome?.Contract;
                var outcomeLocation = manifestedOutcome?.Location;
                var profileResultOutcome = profileResult.Profile.ResolveOperationResult(
                    configuration.SubjectTypeName,
                    operation.ReturnTypeName);
                if (outcomeContract is null && profileResultOutcome is not null)
                {
                    outcomeContract = new DomainOperationOutcomeContract(
                        operation.OperationId,
                        DomainOperationOutcomeKinds.ReturnsResult,
                        new[]
                        {
                            profileResultOutcome.SuccessMemberPath,
                            profileResultOutcome.ValueMemberPath
                        });
                    outcomeLocation = profileResultOutcome.Location;
                }
                else if (outcomeContract is null && profileRejection is not null &&
                         operation.ReturnTypeName == operation.SubjectTypeName)
                {
                    outcomeContract = new DomainOperationOutcomeContract(
                        operation.OperationId,
                        DomainOperationOutcomeKinds.ThrowsException,
                        new[] { profileRejection.ExceptionTypeName });
                    outcomeLocation = profileRejection.Location;
                }

                if (outcomeContract is null)
                    continue;

                var invalidCases = generatedCases
                    .Where(validationCase =>
                        validationCase.ExpectedOutcome == ExpectedValidationOutcome.Invalid &&
                        operation.Parameters.Any(parameter =>
                            parameter.MemberPath == validationCase.Mutation.Property.Name))
                    .GroupBy(validationCase => validationCase.Name)
                    .Select(group => group.First())
                    .ToArray();
                if (invalidCases.Length == 0)
                {
                    context.ReportDiagnostic(
                        GeneratorDiagnostics.ConstrainedOperationParameterMissing(
                            outcomeLocation ?? configuration.Location,
                            operation.OperationId));
                    continue;
                }

                foreach (var invalidCase in invalidCases)
                {
                    var outcomeResolution = DomainOperationOutcomeProviderPipeline.Resolve(
                        new OperationRejectionPlanningRequest(
                            operation,
                            outcomeContract,
                            configuration.BaselineFactoryExpression,
                            invalidCase));
                    switch (outcomeResolution.Kind)
                    {
                        case ProviderResolutionKind.Handled:
                            generatedTests.Add(outcomeResolution.Output!);
                            break;
                        case ProviderResolutionKind.Unhandled:
                            context.ReportDiagnostic(GeneratorDiagnostics.OutcomeProviderMissing(
                                outcomeLocation ?? configuration.Location,
                                outcomeContract.KindId,
                                operation.OperationId));
                            break;
                        case ProviderResolutionKind.Ambiguous:
                            context.ReportDiagnostic(GeneratorDiagnostics.OutcomeProviderAmbiguous(
                                outcomeLocation ?? configuration.Location,
                                outcomeContract.KindId,
                                operation.OperationId,
                                string.Join(", ", outcomeResolution.MatchingProviderIds)));
                            break;
                        case ProviderResolutionKind.Invalid:
                            context.ReportDiagnostic(GeneratorDiagnostics.OutcomeProviderInvalid(
                                outcomeLocation ?? configuration.Location,
                                outcomeResolution.ProviderId!,
                                outcomeContract.KindId,
                                operation.OperationId,
                                outcomeResolution.Reason!));
                            break;
                    }
                }
            }

            var scenarioOutcomes = operationManifests.Outcomes
                .Where(outcome => outcome.SubjectTypeName == configuration.SubjectTypeName)
                .Select(outcome => outcome.Contract)
                .ToList();
            foreach (var operation in constructionOperations)
            {
                if (scenarioOutcomes.Any(outcome => outcome.OperationId == operation.OperationId))
                    continue;
                var resultSpec = profileResult.Profile.ResolveOperationResult(
                    configuration.SubjectTypeName,
                    operation.ReturnTypeName);
                if (resultSpec is not null)
                {
                    scenarioOutcomes.Add(new DomainOperationOutcomeContract(
                        operation.OperationId,
                        DomainOperationOutcomeKinds.ReturnsResult,
                        new[] { resultSpec.SuccessMemberPath, resultSpec.ValueMemberPath }));
                }
            }

            foreach (var scenario in matchingScenarios)
            {
                var scenarioResolution = DomainScenarioProviderPipeline.Resolve(
                    new DomainScenarioPlanningRequest(
                        scenario.Contract,
                        configuration,
                        configuration.EquivalentCopyExpression,
                        constructionOperations,
                        identityMemberPath,
                        matchingRules.Select(rule => rule.Contract).ToArray(),
                        scenarioOutcomes,
                        profileResult.Profile.ConfiguredValues,
                        configuration.InferredValues,
                        nestedTypeName =>
                        {
                            var nestedConfiguration = configurationResult.Configurations
                                .FirstOrDefault(candidate =>
                                    candidate.SubjectTypeName == nestedTypeName);
                            if (nestedConfiguration is null)
                            {
                                return NestedValidInstanceResolution.Uncovered(
                                    $"no generated fixture recipe is available for '{nestedTypeName}'");
                            }

                            var nestedRecipeIdentifier = FixtureFactorySourceEmitter.CreateRecipeIdentifier(
                                nestedConfiguration.RecipeName);
                            return NestedValidInstanceResolution.Covered(
                                $"global::{nestedConfiguration.NamespaceName}.{nestedConfiguration.ConfigurationName}Factory.{nestedRecipeIdentifier}.Create()");
                        }));
                switch (scenarioResolution.Kind)
                {
                    case ProviderResolutionKind.Handled:
                        generatedTests.AddRange(scenarioResolution.Output!);
                        break;
                    case ProviderResolutionKind.Unhandled:
                        context.ReportDiagnostic(GeneratorDiagnostics.ScenarioProviderMissing(
                            scenario.Location ?? configuration.Location,
                            scenario.Contract.KindId,
                            configuration.SubjectTypeName));
                        break;
                    case ProviderResolutionKind.Ambiguous:
                        context.ReportDiagnostic(GeneratorDiagnostics.ScenarioProviderAmbiguous(
                            scenario.Location ?? configuration.Location,
                            scenario.Contract.KindId,
                            configuration.SubjectTypeName,
                            string.Join(", ", scenarioResolution.MatchingProviderIds)));
                        break;
                    case ProviderResolutionKind.Invalid:
                        context.ReportDiagnostic(GeneratorDiagnostics.ScenarioProviderInvalid(
                            scenario.Location ?? configuration.Location,
                            scenarioResolution.ProviderId!,
                            scenario.Contract.KindId,
                            configuration.SubjectTypeName,
                            scenarioResolution.Reason!));
                        break;
                }
            }

            if (generatedTests.Count == 0)
                continue;

            var suite = new GeneratedTestSuite(
                configuration.NamespaceName,
                $"{configuration.SubjectTypeShortName}{configuration.RecipeName}GeneratedTests",
                generatedTests);
            var source = new NUnitTestEmitter().EmitSource(suite) +
                         generatedAdapter +
                         generatedReconstruction;
            var generatedTypeReferences = GeneratedTypeReferenceCollector.Collect(
                    configuration,
                    constructionOperations,
                    scenarioOutcomes,
                    validationRulesTypeKey)
                .ToList();
            generatedTypeReferences.AddRange(configurationResult.Configurations.Select(candidate =>
                $"global::{candidate.NamespaceName}.{candidate.ConfigurationName}Factory"));
            var configuredRejection = profileResult.Profile.ResolveOperationRejection(
                configuration.SubjectTypeName);
            if (configuredRejection is not null)
                generatedTypeReferences.Add(configuredRejection.ExceptionTypeName);
            generatedTypeReferences.Add(UniqueValueSourceEmitter.TypeName);
            source = GeneratedSourceAliasRewriter.Rewrite(
                source,
                generatedTypeReferences,
                configuration.NamespaceName);
            var hintName = $"{configuration.ConfigurationName}.{configuration.RecipeName}.g.cs";

            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static GeneratedValidationCase ApplyReconstruction(
        GeneratedValidationCase validationCase,
        IReadOnlyDictionary<string, PropertyMutationSpec> propertyMutations)
    {
        if (!propertyMutations.TryGetValue(
                validationCase.Mutation.Property.Name,
                out var mutation))
        {
            return validationCase;
        }

        return new GeneratedValidationCase(
            validationCase.Name,
            new PropertyMutationDescriptor(
                validationCase.Mutation.Property,
                validationCase.Mutation.Value,
                SyntaxFactory.ParseExpression(mutation.ReconstructionExpression)),
            validationCase.ExpectedOutcome,
            validationCase.ErrorCode);
    }

    private static bool OperationContractsEqual(
        DomainOperationContract left,
        DomainOperationContract right) =>
        left.SchemaVersion == right.SchemaVersion &&
        left.OperationId == right.OperationId &&
        left.KindId == right.KindId &&
        left.DeclaringTypeName == right.DeclaringTypeName &&
        left.SubjectTypeName == right.SubjectTypeName &&
        left.MemberName == right.MemberName &&
        left.ReturnTypeName == right.ReturnTypeName &&
        left.Parameters.Count == right.Parameters.Count &&
        left.Parameters.Zip(right.Parameters, (first, second) =>
                first.Name == second.Name &&
                first.TypeName == second.TypeName &&
                first.MemberPath == second.MemberPath)
            .All(equal => equal);

}
