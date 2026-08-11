using System;
using System.IO;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.Generation;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class RecipeTransitionGenerationTests
{
    [Test]
    public void FromTransition_ShouldGenerateFreshDerivedStateFactories()
    {
        var compilation = CreateCompilation(ValidStateGraphSource);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics);

        generatorDiagnostics.Should().BeEmpty();
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
        var factory = driver.GetRunResult().Results
            .SelectMany(result => result.GeneratedSources)
            .Single(source => source.HintName.EndsWith("ShipmentFixture.Factory.g.cs"))
            .SourceText
            .ToString();
        factory.Should().Contain("ShipmentFixtureFactory.Pending.Create()");
        factory.Should().Contain(
            "subject.Dispatch(new Token(DomainFixtureUniqueValue.NextString(1, 2147483647)))");
        factory.Should().Contain("ShipmentFixtureFactory.Dispatched.Create()");
        factory.Should().Contain("subject.Deliver()");
    }

    [Test]
    public void FromTransition_ShouldReportAMissingTransition()
    {
        var result = RunGenerator(MissingTransitionSource);

        var diagnostics = string.Join(
            " | ",
            result.Diagnostics.Select(diagnostic =>
                $"{diagnostic.Id}: {diagnostic.GetMessage(null)}"));
        diagnostics.Should().Contain("DFG042")
            .And.Contain("successful transition 'Missing' was not found");
    }

    [Test]
    public void FromTransition_ShouldReportRecipeCycles()
    {
        var result = RunGenerator(CyclicStateGraphSource);

        result.Diagnostics.Count(diagnostic => diagnostic.Id == "DFG042")
            .Should().Be(2);
        result.Diagnostics.Should().OnlyContain(diagnostic =>
            diagnostic.Id == "DFG042" &&
            diagnostic.GetMessage(null).Contains("cyclic"));
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());
        return driver.RunGenerators(CreateCompilation(source)).GetRunResult();
    }

    private static CSharpCompilation CreateCompilation(string source)
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

        return CSharpCompilation.Create(
            $"RecipeTransitionTests_{Guid.NewGuid():N}",
            new[]
            {
                CSharpSyntaxTree.ParseText(
                    source,
                    new CSharpParseOptions(LanguageVersion.CSharp10))
            },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private const string ValidStateGraphSource = @"
using System;
using DomainFixture.Generation;

namespace Example
{
    public sealed record Token(string Value)
    {
        public static Token Default { get; } = new Token(""TRACK-1"");
    }

    public enum ShipmentStatus { Pending, Dispatched, Delivered }

    public sealed class Shipment
    {
        private Shipment(Guid id) => Id = id;
        public Guid Id { get; }
        public ShipmentStatus Status { get; private set; }
        public static Shipment Create(Guid id) => new Shipment(id);
        public void Dispatch(Token token) => Status = ShipmentStatus.Dispatched;
        public void Deliver() => Status = ShipmentStatus.Delivered;
    }

    public sealed class Profile : IFixtureGenerationProfile
    {
        public void Configure(IFixtureGenerationOptions options)
        {
            options.Activation().UseFactories();
        }
    }

    public sealed class ShipmentFixture : IFixtureTestConfiguration<Shipment>
    {
        public void Configure(IFixtureTestBuilder<Shipment> fixture)
        {
            fixture.Recipe(""Pending"")
                .Synthesize()
                .Transition(""Dispatch"", subject => subject.Dispatch(FixtureValue.Auto<Token>()), subject => subject.Status, ShipmentStatus.Dispatched);
            fixture.Recipe(""Dispatched"")
                .FromTransition(""Pending"", ""Dispatch"")
                .Transition(""Deliver"", subject => subject.Deliver(), subject => subject.Status, ShipmentStatus.Delivered);
            fixture.Recipe(""Delivered"")
                .FromTransition(""Dispatched"", ""Deliver"");
        }
    }
}";

    private const string MissingTransitionSource = @"
using System;
using DomainFixture.Generation;

namespace Example
{
    public sealed class Subject
    {
        private Subject(Guid id) => Id = id;
        public Guid Id { get; }
        public int State { get; private set; }
        public static Subject Create(Guid id) => new Subject(id);
        public void Go() => State = 1;
    }

    public sealed class Fixture : IFixtureTestConfiguration<Subject>
    {
        public void Configure(IFixtureTestBuilder<Subject> fixture)
        {
            fixture.Recipe(""Source"").Synthesize().Transition(""Go"", subject => subject.Go(), subject => subject.State, 1);
            fixture.Recipe(""Derived"").FromTransition(""Source"", ""Missing"");
        }
    }
}";

    private const string CyclicStateGraphSource = @"
using DomainFixture.Generation;

namespace Example
{
    public sealed class Subject
    {
        public int State { get; private set; }
        public void Go() => State = 1;
    }

    public sealed class Fixture : IFixtureTestConfiguration<Subject>
    {
        public void Configure(IFixtureTestBuilder<Subject> fixture)
        {
            fixture.Recipe(""A"").FromTransition(""B"", ""Go"").Transition(""Go"", subject => subject.Go(), subject => subject.State, 1);
            fixture.Recipe(""B"").FromTransition(""A"", ""Go"").Transition(""Go"", subject => subject.Go(), subject => subject.State, 1);
        }
    }
}";
}
