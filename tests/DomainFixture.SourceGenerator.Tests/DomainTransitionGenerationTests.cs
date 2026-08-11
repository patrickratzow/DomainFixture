using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.Generation;
using DomainFixture.Pipeline;
using DomainFixture.SourceGenerator.Generation;
using DomainFixture.SourceGenerator.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class DomainTransitionGenerationTests
{
    [Test]
    public void StateTransitionProvider_ShouldGenerateStateAndIdentityTests()
    {
        var transition = CreateTransition("Approve", isRejection: false);
        var configuration = CreateConfiguration(
            ImmutableArray.Create(transition),
            identityMemberPath: "Id");
        var contract = DomainTransitionScenarioContractFactory.Create(
            configuration,
            transition);

        var resolution = DomainScenarioProviderPipeline.Resolve(
            new DomainScenarioPlanningRequest(contract, configuration, null));

        resolution.Kind.Should().Be(ProviderResolutionKind.Handled);
        var generated = resolution.Output!;
        generated.Should().HaveCount(2);
        generated.Select(test => test.Name).Should().Contain(new[]
        {
            "Pending_Transition_Approve_ReachesExpectedState",
            "Pending_Transition_Approve_PreservesIdentity"
        });
        var source = string.Join("\n", generated.SelectMany(test => test.Statements));
        source.Should()
            .Contain("subject.Approve();")
            .And.Contain("subject.Status")
            .And.Contain("RegistrationStatus.Approved")
            .And.Contain("var identity = subject.Id;");
    }

    [Test]
    public void RejectedTransitionProvider_ShouldGenerateSynchronousThrowsTest()
    {
        var transition = CreateTransition("Approve again", isRejection: true);
        var configuration = CreateConfiguration(ImmutableArray.Create(transition));
        var contract = DomainTransitionScenarioContractFactory.Create(
            configuration,
            transition);

        var resolution = DomainScenarioProviderPipeline.Resolve(
            new DomainScenarioPlanningRequest(contract, configuration, null));

        resolution.Kind.Should().Be(ProviderResolutionKind.Handled);
        resolution.Output.Should().ContainSingle()
            .Which.Name.Should().Be("Pending_Transition_Approve_again_IsRejected");
        string.Join("\n", resolution.Output!.Single().Statements).Should()
            .Contain("Assert.Throws<global::System.InvalidOperationException>")
            .And.Contain("subject.Approve()");
    }

    [Test]
    public void ScenarioContracts_ShouldKeepMultipleTransitionsDistinctByName()
    {
        var approve = CreateTransition("Approve", isRejection: false);
        var cancel = CreateTransition("Cancel", isRejection: false, commandName: "Cancel");
        var configuration = CreateConfiguration(ImmutableArray.Create(approve, cancel));

        var contracts = configuration.Transitions
            .Select(transition => DomainTransitionScenarioContractFactory.Create(
                configuration,
                transition))
            .ToArray();

        contracts.Should().OnlyContain(contract =>
            contract.KindId == DomainTransitionScenarioKinds.StateTransition);
        contracts.Select(contract =>
                contract.Parameters[DomainTransitionScenarioContractFactory.ScenarioNameParameter])
            .Should().Equal("Approve", "Cancel");
    }

    [Test]
    public void Parser_ShouldGenerateParameterizedCommand()
    {
        var result = RunGenerator(TransitionSource.Replace(
            "subject => subject.Approve()",
            "subject => subject.ApproveWith(1)"));

        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "DFG039");
        string.Join("\n", result.GeneratedTrees).Should().Contain("subject.ApproveWith(1);");
    }

    [Test]
    public void Parser_ShouldSynthesizeAutoCommandArgument()
    {
        var result = RunGenerator(TransitionSource.Replace(
            "subject => subject.Approve()",
            "subject => subject.ApproveWith(FixtureValue.Auto<int>())"));

        result.Diagnostics.Should().BeEmpty();
        string.Join("\n", result.GeneratedTrees).Should().Contain("subject.ApproveWith(0);");
    }

    [Test]
    public void Parser_ShouldGenerateImmutableTransition()
    {
        var source = TransitionSource
            .Replace(
                "public RegistrationStatus ReadStatus() => Status;",
                "public RegistrationStatus ReadStatus() => Status;\n        public Registration ApproveCopy() => new Registration();")
            .Replace(
                "subject => subject.Approve(), subject => subject.Status",
                "subject => subject.ApproveCopy(), subject => subject.Status");

        var result = RunGenerator(source);

        result.Diagnostics.Should().BeEmpty();
        string.Join("\n", result.GeneratedTrees).Should().Contain("subject = subject.ApproveCopy();");
    }

    [Test]
    public void Parser_ShouldGenerateResultPredicateTransition()
    {
        var source = TransitionSource
            .Replace(
                "public RegistrationStatus ReadStatus() => Status;",
                "public RegistrationStatus ReadStatus() => Status;\n        public bool TryApprove(int value) => value >= 0;")
            .Replace(
                ".Transition(\"Approve\", subject => subject.Approve(), subject => subject.Status, RegistrationStatus.Approved);",
                ".Transition(\"Try approve\", subject => subject.TryApprove(FixtureValue.Auto<int>()), result => result);");

        var result = RunGenerator(source);

        result.Diagnostics.Should().BeEmpty();
        string.Join("\n", result.GeneratedTrees).Should()
            .Contain("var result = subject.TryApprove(0);")
            .And.Contain("predicate(result)");
    }

    [Test]
    public void Parser_ShouldGenerateNamedNestedStateExpectation()
    {
        var source = TransitionSource
            .Replace(
                "public enum RegistrationStatus { Pending, Approved }",
                "public enum RegistrationStatus { Pending, Approved }\n    public sealed class RegistrationDetails { public RegistrationStatus Status { get; set; } }")
            .Replace(
                "public RegistrationStatus Status { get; private set; }",
                "public RegistrationStatus Status { get; private set; }\n        public RegistrationDetails Details { get; } = new RegistrationDetails();")
            .Replace(
                "RegistrationStatus.Approved);",
                "RegistrationStatus.Approved)\n                .State(\"Initial\", subject => subject.Details.Status, RegistrationStatus.Pending);");

        var result = RunGenerator(source);

        result.Diagnostics.Should().BeEmpty();
        string.Join("\n", result.GeneratedTrees).Should()
            .Contain("Pending_State_Initial_Matches")
            .And.Contain("subject.Details.Status");
    }

    [Test]
    public void Parser_ShouldReportDuplicateTransitionNames()
    {
        var source = TransitionSource.Replace(
            ".Transition(\"Approve\", subject => subject.Approve(), subject => subject.Status, RegistrationStatus.Approved);",
            ".Transition(\"Approve\", subject => subject.Approve(), subject => subject.Status, RegistrationStatus.Approved)\n" +
            "                .RejectTransition<InvalidOperationException>(\"Approve\", subject => subject.Approve());");

        var result = RunGenerator(source);

        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "DFG038");
    }

    [Test]
    public void Parser_ShouldReportNonMemberStateSelector()
    {
        var result = RunGenerator(TransitionSource.Replace(
            "subject => subject.Status",
            "subject => subject.ReadStatus()"));

        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "DFG040");
    }

    private static DomainTransitionSpec CreateTransition(
        string name,
        bool isRejection,
        string commandName = "Approve")
    {
        var parameters = ImmutableArray<DomainOperationParameterContract>.Empty;
        var operation = new DomainOperationContract(
            DomainOperationContract.CreateOperationId(
                DomainOperationKinds.InstanceCommand,
                "global::Example.Registration",
                commandName,
                parameters),
            DomainOperationKinds.InstanceCommand,
            "global::Example.Registration",
            "global::Example.Registration",
            commandName,
            "void",
            parameters);
        return new DomainTransitionSpec(
            name,
            operation,
            isRejection ? null : "Status",
            isRejection ? null : "global::Example.RegistrationStatus.Approved",
            isRejection ? "global::System.InvalidOperationException" : null,
            null);
    }

    private static FixtureGenerationSpec CreateConfiguration(
        ImmutableArray<DomainTransitionSpec> transitions,
        string? identityMemberPath = null)
    {
        return new FixtureGenerationSpec(
            "RegistrationFixture",
            "Example.Generated",
            "Pending",
            "global::Example.Registration",
            "Registration",
            "global::Example.RegistrationFixture.Baseline()",
            null,
            null,
            ImmutableArray<SubjectPropertySpec>.Empty,
            false,
            ImmutableArray<SubjectConstructorSpec>.Empty,
            false,
            false,
            null,
            ImmutableArray<DomainOperationContract>.Empty,
            null,
            true,
            transitions,
            identityMemberPath);
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(
            typeof(IFixtureTestConfiguration<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(
            typeof(DomainScenarioContract).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(TestAttribute).Assembly.Location));
        var compilation = CSharpCompilation.Create(
            $"TransitionTests_{Guid.NewGuid():N}",
            new[]
            {
                CSharpSyntaxTree.ParseText(
                    source,
                    new CSharpParseOptions(LanguageVersion.CSharp10))
            },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    private const string TransitionSource = @"
using System;
using DomainFixture.Generation;

namespace Example
{
    public enum RegistrationStatus { Pending, Approved }

    public sealed class Registration
    {
        public RegistrationStatus Status { get; private set; }
        public void Approve() => Status = RegistrationStatus.Approved;
        public void ApproveWith(int value) => Status = RegistrationStatus.Approved;
        public RegistrationStatus ReadStatus() => Status;
    }

    public sealed class RegistrationFixture : IFixtureTestConfiguration<Registration>
    {
        public void Configure(IFixtureTestBuilder<Registration> fixture)
        {
            fixture.Recipe(""Pending"")
                .Baseline(Baseline)
                .Transition(""Approve"", subject => subject.Approve(), subject => subject.Status, RegistrationStatus.Approved);
        }

        public static Registration Baseline() => new();
    }
}";
}
