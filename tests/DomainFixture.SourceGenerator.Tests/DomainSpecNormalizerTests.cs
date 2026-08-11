using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Models;
using DomainFixture.SourceGenerator.Normalization;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class DomainSpecNormalizerTests
{
    [Test]
    public void Normalize_ShouldGroupTypesPreserveRecipesAndUnionReadableProperties()
    {
        var from = CreateOperation("subject.from", "From");
        var configurations = ImmutableArray.Create(
            CreateConfiguration(
                "Valid",
                ImmutableArray.Create(
                    CreateProperty("Value", canRead: true),
                    CreateProperty("Hidden", canRead: false)),
                ImmutableArray.Create(from)),
            CreateConfiguration(
                "Alternate",
                ImmutableArray.Create(
                    CreateProperty("Value", canRead: true),
                    CreateProperty("Realm", canRead: true)),
                ImmutableArray.Create(from)));

        var result = DomainSpecNormalizer.Normalize(
            configurations,
            GenerationProfileSpec.Default,
            ImmutableArray<DiscoveredDomainConstraint>.Empty,
            ImmutableArray<DiscoveredDomainScenario>.Empty,
            EmptyManifests());

        result.Types.Should().ContainSingle();
        var type = result.Types[0];
        type.SubjectTypeName.Should().Be("global::Example.Subject");
        type.Recipes.Select(recipe => recipe.Name).Should().Equal("Valid", "Alternate");
        type.Recipes[0].Configuration.Should().BeSameAs(configurations[0]);
        type.Properties.Select(property => property.Name).Should().Equal("Value", "Realm");
        type.ConstructionOperations.Should().ContainSingle();
        type.ConstructionOperations[0].Provenance.Should().Be(DomainFactProvenance.Inferred);
        type.ConstructionOperations[0].SourceId.Should().Be("SubjectFixture:Valid");
        result.OperationConflicts.Should().BeEmpty();
    }

    [Test]
    public void Normalize_ShouldAugmentWithManifestAndReportConflictingStableIdentity()
    {
        var inferred = CreateOperation("subject.from", "From");
        var identical = new DiscoveredDomainOperation(
            CreateOperation("subject.from", "From"),
            "global::External.Adapter",
            null);
        var additional = new DiscoveredDomainOperation(
            CreateOperation("subject.parse", "Parse"),
            "global::External.Adapter",
            null);
        var conflicting = new DiscoveredDomainOperation(
            CreateOperation("subject.from", "Create"),
            "global::Conflicting.Adapter",
            null);
        var manifests = new DomainOperationManifestExtraction(
            ImmutableArray.Create(identical, additional, conflicting),
            ImmutableArray<DiscoveredDomainOperationOutcome>.Empty,
            ImmutableArray<DomainOperationManifestFailure>.Empty);

        var result = DomainSpecNormalizer.Normalize(
            ImmutableArray.Create(CreateConfiguration(
                "Valid",
                ImmutableArray<SubjectPropertySpec>.Empty,
                ImmutableArray.Create(inferred))),
            GenerationProfileSpec.Default,
            ImmutableArray<DiscoveredDomainConstraint>.Empty,
            ImmutableArray<DiscoveredDomainScenario>.Empty,
            manifests);

        result.Types[0].ConstructionOperations.Select(fact => fact.Value.OperationId)
            .Should().Equal("subject.from", "subject.parse");
        result.Types[0].ConstructionOperations[1].Provenance.Should()
            .Be(DomainFactProvenance.Manifest);
        result.OperationConflicts.Should().ContainSingle();
        result.OperationConflicts[0].OperationId.Should().Be("subject.from");
        result.OperationConflicts[0].Existing.Provenance.Should().Be(DomainFactProvenance.Inferred);
        result.OperationConflicts[0].Candidate.SourceId.Should().Be("global::Conflicting.Adapter");
        result.HasConflicts.Should().BeTrue();
    }

    [Test]
    public void Normalize_ShouldAttachFactsWithSourcesAndApplyOutcomePrecedence()
    {
        var from = CreateOperation("subject.from", "From");
        var parse = CreateOperation("subject.parse", "Parse");
        var manifestedOutcome = new DiscoveredDomainOperationOutcome(
            new DomainOperationOutcomeContract(
                from.OperationId,
                DomainOperationOutcomeKinds.ThrowsException,
                new[] { "global::Example.ManifestException" }),
            "global::External.Adapter",
            "global::Example.Subject",
            null);
        var manifests = new DomainOperationManifestExtraction(
            ImmutableArray.Create(new DiscoveredDomainOperation(
                parse,
                "global::External.Adapter",
                null)),
            ImmutableArray.Create(manifestedOutcome),
            ImmutableArray<DomainOperationManifestFailure>.Empty);
        var profile = CreateProfile(new OperationRejectionSpec(
            "global::Example.Subject",
            "global::Example.ProfileException",
            null));
        var constraint = new DiscoveredDomainConstraint(
            new DomainConstraintContract(
                DomainConstraintKinds.TextNotEmpty,
                "global::Example.Validator",
                "global::Example.Subject",
                "Value"),
            propertyCanBeAssigned: false,
            location: null);
        var scenario = new DiscoveredDomainScenario(
            new DomainScenarioContract(
                "example.scenario",
                "global::Example.ScenarioAdapter",
                "global::Example.Subject"),
            null);

        var result = DomainSpecNormalizer.Normalize(
            ImmutableArray.Create(CreateConfiguration(
                "Valid",
                ImmutableArray<SubjectPropertySpec>.Empty,
                ImmutableArray.Create(from),
                hasEquality: true)),
            profile,
            ImmutableArray.Create(constraint),
            ImmutableArray.Create(scenario),
            manifests);

        var type = result.Types[0];
        type.Constraints.Should().ContainSingle();
        type.Constraints[0].SourceId.Should().Be("global::Example.Validator");
        type.Scenarios.Should().Contain(fact =>
            fact.Value.KindId == "example.scenario" &&
            fact.SourceId == "global::Example.ScenarioAdapter");
        type.Scenarios.Should().Contain(fact =>
            fact.Value.KindId == DomainScenarioKinds.ValueObjectEquality &&
            fact.Provenance == DomainFactProvenance.Inferred);
        type.Scenarios.Should().Contain(fact =>
            fact.Value.KindId == DomainScenarioKinds.ConstructionRoundTrip);

        type.Outcomes.Should().HaveCount(2);
        type.Outcomes.Single(fact => fact.Value.OperationId == "subject.from")
            .Value.Parameters.Should().Equal("global::Example.ManifestException");
        var profileOutcome = type.Outcomes.Single(fact =>
            fact.Value.OperationId == "subject.parse");
        profileOutcome.Value.Parameters.Should().Equal("global::Example.ProfileException");
        profileOutcome.Provenance.Should().Be(DomainFactProvenance.Profile);
    }

    [Test]
    public void Normalize_ShouldRetainManifestOnlySubjectAndFailures()
    {
        var operation = CreateOperation("subject.parse", "Parse");
        var failure = new DomainOperationManifestFailure(
            DomainOperationManifestFailureKind.MalformedOperationManifest,
            "broken",
            "broken operation");

        var result = DomainSpecNormalizer.Normalize(
            ImmutableArray<FixtureGenerationSpec>.Empty,
            GenerationProfileSpec.Default,
            ImmutableArray<DiscoveredDomainConstraint>.Empty,
            ImmutableArray<DiscoveredDomainScenario>.Empty,
            new DomainOperationManifestExtraction(
                ImmutableArray.Create(new DiscoveredDomainOperation(
                    operation,
                    "global::External.Adapter",
                    null)),
                ImmutableArray<DiscoveredDomainOperationOutcome>.Empty,
                ImmutableArray.Create(failure)));

        result.Types.Should().ContainSingle();
        result.Types[0].Recipes.Should().BeEmpty();
        result.Types[0].ConstructionOperations.Should().ContainSingle();
        result.ManifestFailures.Should().ContainSingle().Which.Should().BeSameAs(failure);
        result.HasConflicts.Should().BeTrue();
    }

    private static DomainOperationManifestExtraction EmptyManifests() => new(
        ImmutableArray<DiscoveredDomainOperation>.Empty,
        ImmutableArray<DiscoveredDomainOperationOutcome>.Empty,
        ImmutableArray<DomainOperationManifestFailure>.Empty);

    private static DomainOperationContract CreateOperation(string id, string memberName) => new(
        id,
        DomainOperationKinds.StaticFactory,
        "global::Example.Subject",
        "global::Example.Subject",
        memberName,
        "global::Example.Subject",
        new[]
        {
            new DomainOperationParameterContract("value", "global::System.String", "Value")
        });

    private static SubjectPropertySpec CreateProperty(string name, bool canRead) => new(
        name,
        "global::System.String",
        isString: true,
        isNonNullable: true,
        canBeAssigned: false,
        canSetInObjectInitializer: false,
        hasSetter: false,
        canSetFromDerivedType: false,
        canReadFromGeneratedCode: canRead);

    private static FixtureGenerationSpec CreateConfiguration(
        string recipeName,
        ImmutableArray<SubjectPropertySpec> properties,
        ImmutableArray<DomainOperationContract> operations,
        bool hasEquality = false) => new(
        "SubjectFixture",
        "Example.Generated",
        recipeName,
        "global::Example.Subject",
        "Subject",
        "global::Example.SubjectFixture.Baseline()",
        validatorFactoryExpression: null,
        validationRulesTypeKey: null,
        properties,
        isRecord: false,
        ImmutableArray<SubjectConstructorSpec>.Empty,
        canUseDerivedReconstruction: false,
        hasValueEqualitySemantics: hasEquality,
        equivalentCopyExpression: null,
        operations,
        location: null);

    private static GenerationProfileSpec CreateProfile(params OperationRejectionSpec[] rejections) => new(
        useNullability: false,
        usePropertyNames: false,
        useImmutableObjects: false,
        useEntityIdentity: false,
        useFluentValidation: false,
        FixtureActivationKind.Factories,
        serviceProviderFactoryType: null,
        ImmutableArray<PropertyMutationSpec>.Empty,
        rejections.ToImmutableArray());
}
