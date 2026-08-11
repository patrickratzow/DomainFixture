using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Generation;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Normalization;

internal static class DomainSpecNormalizer
{
    public static DomainSpecNormalizationResult Normalize(
        ImmutableArray<FixtureGenerationSpec> configurations,
        GenerationProfileSpec profile,
        ImmutableArray<DiscoveredDomainConstraint> constraints,
        ImmutableArray<DiscoveredDomainScenario> scenarios,
        DomainOperationManifestExtraction operationManifests)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        if (operationManifests is null) throw new ArgumentNullException(nameof(operationManifests));

        configurations = configurations.IsDefault
            ? ImmutableArray<FixtureGenerationSpec>.Empty
            : configurations;
        constraints = constraints.IsDefault
            ? ImmutableArray<DiscoveredDomainConstraint>.Empty
            : constraints;
        scenarios = scenarios.IsDefault
            ? ImmutableArray<DiscoveredDomainScenario>.Empty
            : scenarios;

        var subjectOrder = new List<string>();
        var seenSubjects = new HashSet<string>(StringComparer.Ordinal);
        void AddSubject(string subjectTypeName)
        {
            if (seenSubjects.Add(subjectTypeName))
                subjectOrder.Add(subjectTypeName);
        }

        foreach (var configuration in configurations)
            AddSubject(configuration.SubjectTypeName);
        foreach (var constraint in constraints)
            AddSubject(constraint.Contract.SubjectTypeName);
        foreach (var scenario in scenarios)
            AddSubject(scenario.Contract.SubjectTypeName);
        foreach (var operation in operationManifests.Operations)
            AddSubject(operation.Contract.SubjectTypeName);
        foreach (var outcome in operationManifests.Outcomes)
            AddSubject(outcome.SubjectTypeName);

        var conflicts = ImmutableArray.CreateBuilder<DomainOperationConflict>();
        var types = ImmutableArray.CreateBuilder<DomainTypeSpec>(subjectOrder.Count);
        foreach (var subjectTypeName in subjectOrder)
        {
            var subjectConfigurations = configurations
                .Where(configuration => configuration.SubjectTypeName == subjectTypeName)
                .ToArray();
            var recipes = subjectConfigurations
                .Select(configuration => new DomainRecipeSpec(configuration))
                .ToImmutableArray();
            var properties = UnionReadableProperties(subjectConfigurations);
            var normalizedOperations = MergeOperations(
                subjectTypeName,
                subjectConfigurations,
                profile,
                operationManifests.Operations,
                operationManifests.Outcomes,
                conflicts);
            var normalizedConstraints = constraints
                .Where(constraint => constraint.Contract.SubjectTypeName == subjectTypeName)
                .Select(constraint => new DomainFact<DomainConstraintContract>(
                    constraint.Contract,
                    DomainFactProvenance.Discovered,
                    constraint.Contract.SourceTypeName,
                    constraint.Location))
                .ToImmutableArray();
            var normalizedOutcomes = NormalizeOutcomes(
                subjectTypeName,
                profile,
                normalizedOperations,
                operationManifests.Outcomes);
            var allOperations = IncludeBehaviorOperations(
                subjectConfigurations,
                normalizedOperations);
            var normalizedScenarios = NormalizeScenarios(
                subjectTypeName,
                subjectConfigurations,
                normalizedOperations,
                scenarios);

            types.Add(new DomainTypeSpec(
                subjectTypeName,
                recipes,
                properties,
                normalizedConstraints,
                normalizedOperations,
                normalizedOutcomes,
                normalizedScenarios,
                allOperations));
        }

        return new DomainSpecNormalizationResult(
            types.ToImmutable(),
            conflicts.ToImmutable(),
            operationManifests.Failures);
    }

    private static ImmutableArray<DomainFact<DomainOperationContract>> IncludeBehaviorOperations(
        IEnumerable<FixtureGenerationSpec> configurations,
        ImmutableArray<DomainFact<DomainOperationContract>> constructionOperations)
    {
        var operations = constructionOperations.ToBuilder();
        var ids = new HashSet<string>(
            constructionOperations.Select(operation => operation.Value.OperationId),
            StringComparer.Ordinal);
        foreach (var configuration in configurations)
        {
            var sourceId = $"{configuration.ConfigurationName}:{configuration.RecipeName}";
            foreach (var transition in configuration.Transitions)
            {
                if (!ids.Add(transition.Operation.OperationId))
                    continue;
                operations.Add(new DomainFact<DomainOperationContract>(
                    transition.Operation,
                    DomainFactProvenance.Inferred,
                    sourceId,
                    transition.Location ?? configuration.Location));
            }
        }

        return operations.ToImmutable();
    }

    private static ImmutableArray<SubjectPropertySpec> UnionReadableProperties(
        IEnumerable<FixtureGenerationSpec> configurations)
    {
        var properties = ImmutableArray.CreateBuilder<SubjectPropertySpec>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var configuration in configurations)
        {
            foreach (var property in configuration.SubjectProperties)
            {
                if (property.CanReadFromGeneratedCode && names.Add(property.Name))
                    properties.Add(property);
            }
        }

        return properties.ToImmutable();
    }

    private static ImmutableArray<DomainFact<DomainOperationContract>> MergeOperations(
        string subjectTypeName,
        IEnumerable<FixtureGenerationSpec> configurations,
        GenerationProfileSpec profile,
        ImmutableArray<DiscoveredDomainOperation> manifestedOperations,
        ImmutableArray<DiscoveredDomainOperationOutcome> manifestedOutcomes,
        ImmutableArray<DomainOperationConflict>.Builder conflicts)
    {
        var operations = ImmutableArray.CreateBuilder<DomainFact<DomainOperationContract>>();
        var byId = new Dictionary<string, DomainFact<DomainOperationContract>>(StringComparer.Ordinal);

        foreach (var configuration in configurations)
        {
            var sourceId = $"{configuration.ConfigurationName}:{configuration.RecipeName}";
            foreach (var operation in configuration.ConstructionOperations)
            {
                AddOperation(new DomainFact<DomainOperationContract>(
                    operation,
                    DomainFactProvenance.Inferred,
                    sourceId,
                    configuration.Location));
            }
        }

        foreach (var manifested in manifestedOperations.Where(operation =>
                     operation.Contract.SubjectTypeName == subjectTypeName &&
                     (operation.Contract.ReturnTypeName == subjectTypeName ||
                      profile.ResolveOperationResult(
                          subjectTypeName,
                          operation.Contract.ReturnTypeName) is not null ||
                      manifestedOutcomes.Any(outcome =>
                          outcome.SubjectTypeName == subjectTypeName &&
                          outcome.Contract.OperationId == operation.Contract.OperationId &&
                          outcome.Contract.KindId == DomainOperationOutcomeKinds.ReturnsResult)) &&
                     operation.Contract.KindId is DomainOperationKinds.Constructor or
                         DomainOperationKinds.StaticFactory))
        {
            AddOperation(new DomainFact<DomainOperationContract>(
                manifested.Contract,
                DomainFactProvenance.Manifest,
                manifested.SourceTypeName,
                manifested.Location));
        }

        return operations.ToImmutable();

        void AddOperation(DomainFact<DomainOperationContract> candidate)
        {
            var operationId = candidate.Value.OperationId;
            if (!byId.TryGetValue(operationId, out var existing))
            {
                byId.Add(operationId, candidate);
                operations.Add(candidate);
                return;
            }

            if (!OperationsEqual(existing.Value, candidate.Value))
            {
                conflicts.Add(new DomainOperationConflict(
                    subjectTypeName,
                    operationId,
                    existing,
                    candidate));
            }
        }
    }

    private static ImmutableArray<DomainFact<DomainOperationOutcomeContract>> NormalizeOutcomes(
        string subjectTypeName,
        GenerationProfileSpec profile,
        ImmutableArray<DomainFact<DomainOperationContract>> operations,
        ImmutableArray<DiscoveredDomainOperationOutcome> manifestedOutcomes)
    {
        var outcomes = ImmutableArray.CreateBuilder<DomainFact<DomainOperationOutcomeContract>>();
        var manifestedByOperationId = new HashSet<string>(StringComparer.Ordinal);
        foreach (var manifested in manifestedOutcomes.Where(outcome =>
                     outcome.SubjectTypeName == subjectTypeName))
        {
            outcomes.Add(new DomainFact<DomainOperationOutcomeContract>(
                manifested.Contract,
                DomainFactProvenance.Manifest,
                manifested.SourceTypeName,
                manifested.Location));
            manifestedByOperationId.Add(manifested.Contract.OperationId);
        }

        foreach (var operation in operations)
        {
            if (manifestedByOperationId.Contains(operation.Value.OperationId))
                continue;

            var result = profile.ResolveOperationResult(
                subjectTypeName,
                operation.Value.ReturnTypeName);
            if (result is not null)
            {
                outcomes.Add(new DomainFact<DomainOperationOutcomeContract>(
                    new DomainOperationOutcomeContract(
                        operation.Value.OperationId,
                        DomainOperationOutcomeKinds.ReturnsResult,
                        new[] { result.SuccessMemberPath, result.ValueMemberPath }),
                    DomainFactProvenance.Profile,
                    result.ResultTypeName,
                    result.Location));
                continue;
            }

            var rejection = profile.ResolveOperationRejection(subjectTypeName);
            if (rejection is null ||
                operation.Value.ReturnTypeName != subjectTypeName)
                continue;

            outcomes.Add(new DomainFact<DomainOperationOutcomeContract>(
                new DomainOperationOutcomeContract(
                    operation.Value.OperationId,
                    DomainOperationOutcomeKinds.ThrowsException,
                    new[] { rejection.ExceptionTypeName }),
                DomainFactProvenance.Profile,
                rejection.SubjectTypeKey ?? "assembly",
                rejection.Location));
        }

        return outcomes.ToImmutable();
    }

    private static ImmutableArray<DomainFact<DomainScenarioContract>> NormalizeScenarios(
        string subjectTypeName,
        IReadOnlyList<FixtureGenerationSpec> configurations,
        ImmutableArray<DomainFact<DomainOperationContract>> operations,
        ImmutableArray<DiscoveredDomainScenario> discoveredScenarios)
    {
        var normalized = ImmutableArray.CreateBuilder<DomainFact<DomainScenarioContract>>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scenario in discoveredScenarios.Where(candidate =>
                     candidate.Contract.SubjectTypeName == subjectTypeName))
        {
            AddScenario(new DomainFact<DomainScenarioContract>(
                scenario.Contract,
                DomainFactProvenance.Discovered,
                scenario.Contract.SourceTypeName,
                scenario.Location));
        }

        foreach (var configuration in configurations)
        {
            var sourceId = $"{configuration.ConfigurationName}:{configuration.RecipeName}";
            if (configuration.HasValueEqualitySemantics)
            {
                AddScenario(new DomainFact<DomainScenarioContract>(
                    new DomainScenarioContract(
                        DomainScenarioKinds.ValueObjectEquality,
                        subjectTypeName,
                        subjectTypeName),
                    DomainFactProvenance.Inferred,
                    sourceId,
                    configuration.Location));
            }

            foreach (var transition in configuration.Transitions)
            {
                AddScenario(new DomainFact<DomainScenarioContract>(
                    DomainTransitionScenarioContractFactory.Create(configuration, transition),
                    DomainFactProvenance.Inferred,
                    sourceId,
                    transition.Location ?? configuration.Location));
            }

            foreach (var state in configuration.StateExpectations)
            {
                AddScenario(new DomainFact<DomainScenarioContract>(
                    DomainTransitionScenarioContractFactory.CreateState(configuration, state),
                    DomainFactProvenance.Inferred,
                    sourceId,
                    state.Location ?? configuration.Location));
            }
        }

        if (operations.Length > 0)
        {
            AddScenario(new DomainFact<DomainScenarioContract>(
                new DomainScenarioContract(
                    DomainScenarioKinds.ConstructionRoundTrip,
                    subjectTypeName,
                    subjectTypeName),
                DomainFactProvenance.Inferred,
                subjectTypeName,
                configurations.FirstOrDefault()?.Location));
        }

        return normalized.ToImmutable();

        void AddScenario(DomainFact<DomainScenarioContract> scenario)
        {
            var key = CreateScenarioKey(scenario.Value);
            if (keys.Add(key))
                normalized.Add(scenario);
        }
    }

    private static string CreateScenarioKey(DomainScenarioContract scenario)
    {
        var parameters = string.Join(";", scenario.Parameters
            .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
            .Select(parameter => $"{parameter.Key}={parameter.Value}"));
        return $"{scenario.KindId}|{parameters}";
    }

    private static bool OperationsEqual(DomainOperationContract left, DomainOperationContract right) =>
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
