using System.Collections.Immutable;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.Pipeline;
using DomainFixture.SourceGenerator.Generation;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class DomainOperationOutcomeProviderPipelineTests
{
    [Test]
    public void Resolve_ShouldReplaceOnlyTargetedArgumentAndAssertSynchronousException()
    {
        var operation = CreateFactoryOperation();
        var invalidCase = new GeneratedValidationCase(
            "Name_NotEmpty_Empty_IsInvalid",
            new PropertyMutationDescriptor(
                new PropertyDescriptor("Name"),
                SyntaxFactory.ParseExpression("string.Empty")),
            ExpectedValidationOutcome.Invalid);

        var resolution = DomainOperationOutcomeProviderPipeline.Resolve(
            new OperationRejectionPlanningRequest(
                operation,
                new DomainOperationOutcomeContract(
                    operation.OperationId,
                    DomainOperationOutcomeKinds.ThrowsException,
                    new[] { "global::Example.DomainException" }),
                "global::Example.QualifiedName.Valid()",
                invalidCase));

        resolution.Kind.Should().Be(ProviderResolutionKind.Handled);
        resolution.ProviderId.Should().Be("domainfixture.outcomes.synchronous-exception");
        resolution.Output!.Name.Should()
            .Be("Construction_From_Name_NotEmpty_Empty_IsInvalid_IsRejected");
        var source = string.Join("\n", resolution.Output.Statements.Select(statement => statement.ToFullString()));
        source.Should()
            .Contain("global::Example.QualifiedName baseline = global::Example.QualifiedName.Valid();")
            .And.Contain("Assert.Throws<global::Example.DomainException>")
            .And.Contain("global::Example.QualifiedName.From(string.Empty, baseline.Realm)");
    }

    [Test]
    public void Resolve_ShouldSupportConstructorRejection()
    {
        var operation = new DomainOperationContract(
            "qualified-name.constructor",
            DomainOperationKinds.Constructor,
            "global::Example.QualifiedName",
            "global::Example.QualifiedName",
            ".ctor",
            "global::Example.QualifiedName",
            new[]
            {
                new DomainOperationParameterContract("name", "global::System.String", "Name")
            });
        var invalidCase = CreateInvalidCase("Name");

        var resolution = DomainOperationOutcomeProviderPipeline.Resolve(
            new OperationRejectionPlanningRequest(
                operation,
                CreateOutcome(operation),
                "global::Example.QualifiedName.Valid()",
                invalidCase));

        resolution.Kind.Should().Be(ProviderResolutionKind.Handled);
        resolution.Output!.Statements.Last().ToFullString().Should()
            .Contain("new global::Example.QualifiedName(null)");
    }

    [Test]
    public void Resolve_ShouldRejectAValidBoundaryCase()
    {
        var operation = CreateFactoryOperation();
        var validCase = new GeneratedValidationCase(
            "Name_LengthAtMaximum_IsValid",
            new PropertyMutationDescriptor(
                new PropertyDescriptor("Name"),
                SyntaxFactory.ParseExpression("\"valid\"")),
            ExpectedValidationOutcome.Valid);

        var resolution = DomainOperationOutcomeProviderPipeline.Resolve(
            new OperationRejectionPlanningRequest(
                operation,
                CreateOutcome(operation),
                "global::Example.QualifiedName.Valid()",
                validCase));

        resolution.Kind.Should().Be(ProviderResolutionKind.Invalid);
        resolution.Reason.Should().Contain("invalid boundary case");
    }

    [Test]
    public void Resolve_ShouldRejectAnUnmappedBoundary()
    {
        var operation = CreateFactoryOperation();

        var resolution = DomainOperationOutcomeProviderPipeline.Resolve(
            new OperationRejectionPlanningRequest(
                operation,
                CreateOutcome(operation),
                "global::Example.QualifiedName.Valid()",
                CreateInvalidCase("Unknown")));

        resolution.Kind.Should().Be(ProviderResolutionKind.Invalid);
        resolution.Reason.Should().Contain("has no parameter mapped to 'Unknown'");
    }

    [Test]
    public void Resolve_ShouldLeaveUnknownOutcomeKindsUnhandled()
    {
        var operation = CreateFactoryOperation();

        var resolution = DomainOperationOutcomeProviderPipeline.Resolve(
            new OperationRejectionPlanningRequest(
                operation,
                new DomainOperationOutcomeContract(
                    operation.OperationId,
                    "example.outcome.result",
                    ImmutableArray<string>.Empty),
                "global::Example.QualifiedName.Valid()",
                CreateInvalidCase("Name")));

        resolution.Kind.Should().Be(ProviderResolutionKind.Unhandled);
    }

    private static DomainOperationContract CreateFactoryOperation() => new(
        "qualified-name.from",
        DomainOperationKinds.StaticFactory,
        "global::Example.QualifiedName",
        "global::Example.QualifiedName",
        "From",
        "global::Example.QualifiedName",
        new[]
        {
            new DomainOperationParameterContract("name", "global::System.String", "Name"),
            new DomainOperationParameterContract("realm", "global::System.String", "Realm")
        });

    private static DomainOperationOutcomeContract CreateOutcome(DomainOperationContract operation) => new(
        operation.OperationId,
        DomainOperationOutcomeKinds.ThrowsException,
        new[] { "global::Example.DomainException" });

    private static GeneratedValidationCase CreateInvalidCase(string propertyName) => new(
        $"{propertyName}_NotNull_Null_IsInvalid",
        new PropertyMutationDescriptor(
            new PropertyDescriptor(propertyName),
            SyntaxFactory.ParseExpression("null")),
        ExpectedValidationOutcome.Invalid);
}
