using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Emission;
using DomainFixture.SourceGenerator.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class DomainSpecVisibilityEmitterTests
{
    [Test]
    public void SnapshotEmitter_ShouldListNormalizedFactsAndProvenanceDeterministically()
    {
        var source = DomainSpecSnapshotEmitter.EmitSource(CreateNormalization());
        var reordered = DomainSpecSnapshotEmitter.EmitSource(CreateNormalization(reverseTypes: true));

        source.Should().Be(reordered);
        source.Should()
            .Contain("// TYPE global::Example.Orphan")
            .And.Contain("// TYPE global::Example.User")
            .And.Contain("RECIPE Validation | configuration=UserFixture")
            .And.Contain("PROPERTY Value : string")
            .And.Contain("CONSTRAINT Value | kind=domainfixture.text.minimum-length | parameters=minimum=3")
            .And.Contain("OPERATION domainfixture.operation.static-factory:global::Example.User.From(string)")
            .And.Contain("OUTCOME domainfixture.operation.static-factory:global::Example.User.From(string)")
            .And.Contain("SCENARIO domainfixture.law.value-object-equality")
            .And.Contain("provenance=Manifest | source=global::Example.Adapter")
            .And.NotContain("NUnit")
            .And.NotContain("FluentValidation")
            .And.NotContain("System.Reflection");
        ParseErrors(source).Should().BeEmpty();
    }

    [Test]
    public void CoverageEmitter_ShouldClassifyEveryFactAndExposeTotals()
    {
        var normalization = CreateNormalization();

        var report = DomainCoverageReportEmitter.Analyze(normalization);
        var source = DomainCoverageReportEmitter.EmitSource(normalization);

        report.Total.Should().Be(7);
        report.Factory.Should().Be(0);
        report.Test.Should().Be(4);
        report.Both.Should().Be(2);
        report.Uncovered.Should().Be(1);
        report.Facts.Should().Contain(fact =>
            fact.Category == "Recipe" &&
            fact.Classification == DomainCoverageClassification.Both);
        report.Facts.Should().Contain(fact =>
            fact.Category == "Property" &&
            fact.Classification == DomainCoverageClassification.Both);
        report.Facts.Should().Contain(fact =>
            fact.SubjectTypeName == "global::Example.Orphan" &&
            fact.Category == "Constraint" &&
            fact.Classification == DomainCoverageClassification.Uncovered &&
            fact.Reason.Contains("no recipe"));
        source.Should()
            .Contain("[Both] Recipe UserFixture:Validation")
            .And.Contain("[Test] Outcome")
            .And.Contain("[Uncovered] Constraint Value:domainfixture.text.minimum-length")
            .And.Contain("TOTAL facts=7 factory=0 test=4 both=2 uncovered=1")
            .And.Contain("internal const int Uncovered = 1;");
        ParseErrors(source).Should().BeEmpty();
    }

    [Test]
    public void CoverageEmitter_ShouldAttributeManifestOnlySynthesisToChosenFacts()
    {
        var constraint = CreateConstraint("global::Example.Synthesized", "global::Example.SynthesizedRules");
        var operation = CreateOperation("global::Example.Synthesized");
        var type = new DomainTypeSpec(
            "global::Example.Synthesized",
            ImmutableArray<DomainRecipeSpec>.Empty,
            ImmutableArray<SubjectPropertySpec>.Empty,
            ImmutableArray.Create(new DomainFact<DomainConstraintContract>(
                constraint,
                DomainFactProvenance.Manifest,
                "global::Example.SynthesizedRules",
                location: null)),
            ImmutableArray.Create(new DomainFact<DomainOperationContract>(
                operation,
                DomainFactProvenance.Manifest,
                "global::Example.SynthesizedAdapter",
                location: null)),
            ImmutableArray<DomainFact<DomainOperationOutcomeContract>>.Empty,
            ImmutableArray<DomainFact<DomainScenarioContract>>.Empty);
        var normalization = new DomainSpecNormalizationResult(
            ImmutableArray.Create(type),
            ImmutableArray<DomainOperationConflict>.Empty,
            ImmutableArray<DomainOperationManifestFailure>.Empty);

        var report = DomainCoverageReportEmitter.Analyze(normalization);

        report.Factory.Should().Be(2);
        report.Uncovered.Should().Be(0);
        report.Facts.Should().OnlyContain(fact =>
            fact.Classification == DomainCoverageClassification.Factory);
        report.Facts.Single(fact => fact.Category == "Operation").Reason.Should()
            .Contain("synthesized factory invokes");
        report.Facts.Single(fact => fact.Category == "Constraint").Reason.Should()
            .Contain("derives a valid argument");
    }

    [Test]
    public void SnapshotEmitter_ShouldExposeConfiguredValues()
    {
        var profile = new GenerationProfileSpec(
            false, false, false, false, false,
            FixtureActivationKind.Factories,
            null,
            ImmutableArray<PropertyMutationSpec>.Empty,
            ImmutableArray<OperationRejectionSpec>.Empty,
            ImmutableArray<OperationResultSpec>.Empty,
            ImmutableArray.Create(new ConfiguredValueSpec(
                "global::Example.Currency",
                "global::Example.Currency.Eur",
                null)));

        DomainSpecSnapshotEmitter.EmitSource(CreateNormalization(), profile)
            .Should().Contain(
                "// CONFIGURED VALUE global::Example.Currency = global::Example.Currency.Eur");
    }

    private static DomainSpecNormalizationResult CreateNormalization(bool reverseTypes = false)
    {
        var subjectType = "global::Example.User";
        var property = new SubjectPropertySpec(
            "Value",
            "string",
            isString: true,
            isNonNullable: true,
            canBeAssigned: true,
            canSetInObjectInitializer: true,
            hasSetter: true,
            canSetFromDerivedType: true,
            canReadFromGeneratedCode: true);
        var operation = CreateOperation(subjectType);
        var constraint = CreateConstraint(subjectType, "global::Example.UserRules");
        var configuration = new FixtureGenerationSpec(
            "UserFixture",
            "Example",
            "Validation",
            subjectType,
            "User",
            "global::Example.UserFixture.Baseline()",
            validatorFactoryExpression: null,
            validationRulesTypeKey: "global::Example.UserRules",
            ImmutableArray.Create(property),
            isRecord: false,
            ImmutableArray<SubjectConstructorSpec>.Empty,
            canUseDerivedReconstruction: false,
            hasValueEqualitySemantics: true,
            equivalentCopyExpression: null,
            ImmutableArray.Create(operation),
            location: null,
            canExposePublicFactory: true);
        var user = new DomainTypeSpec(
            subjectType,
            ImmutableArray.Create(new DomainRecipeSpec(configuration)),
            ImmutableArray.Create(property),
            ImmutableArray.Create(new DomainFact<DomainConstraintContract>(
                constraint,
                DomainFactProvenance.Discovered,
                "global::Example.UserRules",
                location: null)),
            ImmutableArray.Create(new DomainFact<DomainOperationContract>(
                operation,
                DomainFactProvenance.Manifest,
                "global::Example.Adapter",
                location: null)),
            ImmutableArray.Create(new DomainFact<DomainOperationOutcomeContract>(
                new DomainOperationOutcomeContract(
                    operation.OperationId,
                    DomainOperationOutcomeKinds.ThrowsException,
                    new[] { "global::System.ArgumentException" }),
                DomainFactProvenance.Manifest,
                "global::Example.Adapter",
                location: null)),
            ImmutableArray.Create(new DomainFact<DomainScenarioContract>(
                new DomainScenarioContract(
                    DomainScenarioKinds.ValueObjectEquality,
                    subjectType,
                    subjectType),
                DomainFactProvenance.Inferred,
                "UserFixture:Validation",
                location: null)));
        var orphan = new DomainTypeSpec(
            "global::Example.Orphan",
            ImmutableArray<DomainRecipeSpec>.Empty,
            ImmutableArray<SubjectPropertySpec>.Empty,
            ImmutableArray.Create(new DomainFact<DomainConstraintContract>(
                CreateConstraint("global::Example.Orphan", "global::Example.OrphanRules"),
                DomainFactProvenance.Manifest,
                "global::Example.OrphanRules",
                location: null)),
            ImmutableArray<DomainFact<DomainOperationContract>>.Empty,
            ImmutableArray<DomainFact<DomainOperationOutcomeContract>>.Empty,
            ImmutableArray<DomainFact<DomainScenarioContract>>.Empty);
        var types = reverseTypes
            ? ImmutableArray.Create(user, orphan)
            : ImmutableArray.Create(orphan, user);
        return new DomainSpecNormalizationResult(
            types,
            ImmutableArray<DomainOperationConflict>.Empty,
            ImmutableArray<DomainOperationManifestFailure>.Empty);
    }

    private static DomainConstraintContract CreateConstraint(
        string subjectType,
        string sourceType) =>
        new(
            DomainConstraintKinds.TextMinimumLength,
            sourceType,
            subjectType,
            "Value",
            new Dictionary<string, string> { [DomainConstraintParameters.Minimum] = "3" },
            "VALUE_TOO_SHORT");

    private static DomainOperationContract CreateOperation(string subjectType) =>
        new(
            DomainOperationKinds.StaticFactory,
            subjectType,
            subjectType,
            "From",
            new[]
            {
                new DomainOperationParameterContract("value", "string", "Value")
            });

    private static IEnumerable<Diagnostic> ParseErrors(string source) =>
        CSharpSyntaxTree.ParseText(source).GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
