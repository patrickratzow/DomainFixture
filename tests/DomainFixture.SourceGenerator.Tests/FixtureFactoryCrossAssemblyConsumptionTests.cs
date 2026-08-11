using System;
using System.Collections.Generic;
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
public sealed class FixtureFactoryCrossAssemblyConsumptionTests
{
    [Test]
    public void PublicFactory_ShouldCompileAndBeConsumedFromSeparateAssembly()
    {
        var supportCompilation = CreateCompilation(
            "Example.TestSupport",
            TestSupportSource,
            CreateFrameworkReferences());
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());

        driver = driver.RunGeneratorsAndUpdateCompilation(
            supportCompilation,
            out var generatedSupportCompilation,
            out var generatorDiagnostics);

        generatorDiagnostics.Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
        generatedSupportCompilation.GetDiagnostics().Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();

        var factorySource = driver.GetRunResult().GeneratedTrees
            .Single(tree => tree.FilePath.EndsWith(
                "Example.Support.PersonFixture.Factory.g.cs",
                StringComparison.Ordinal))
            .ToString();
        factorySource.Should()
            .Contain("public static class PersonFixtureFactory")
            .And.Contain("public static class Valid")
            .And.Contain("public static Person Create()")
            .And.Contain("Func<Person, Person> transform")
            .And.NotContain("IServiceProvider")
            .And.NotContain("GetService")
            .And.NotContain("System.Reflection")
            .And.NotContain("Dictionary")
            .And.NotContain("scenarioName");

        using var supportAssembly = new MemoryStream();
        var supportEmit = generatedSupportCompilation.Emit(supportAssembly);
        supportEmit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, supportEmit.Diagnostics));

        var consumerReferences = CreateFrameworkReferences().ToList();
        consumerReferences.Add(MetadataReference.CreateFromImage(
            supportAssembly.ToArray()));
        var consumerCompilation = CreateCompilation(
            "Example.ConsumerTests",
            ConsumerSource,
            consumerReferences);

        consumerCompilation.GetDiagnostics().Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
        using var consumerAssembly = new MemoryStream();
        var consumerEmit = consumerCompilation.Emit(consumerAssembly);
        consumerEmit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, consumerEmit.Diagnostics));
    }

    private static CSharpCompilation CreateCompilation(
        string assemblyName,
        string source,
        IEnumerable<MetadataReference> references)
    {
        return CSharpCompilation.Create(
            assemblyName,
            new[]
            {
                CSharpSyntaxTree.ParseText(
                    source,
                    new CSharpParseOptions(LanguageVersion.CSharp10))
            },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IEnumerable<MetadataReference> CreateFrameworkReferences()
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
        return references;
    }

    private const string TestSupportSource = @"
using DomainFixture.Generation;

namespace Example.Support
{
    public sealed record Person(string Name);

    public sealed class PersonFixture : IFixtureTestConfiguration<Person>
    {
        public void Configure(IFixtureTestBuilder<Person> fixture)
        {
            fixture.Recipe(""Valid"")
                .Baseline(Baseline);
        }

        public static Person Baseline() => new(""baseline"");
    }
}";

    private const string ConsumerSource = @"
namespace Example.Consumer
{
    public static class HandwrittenTests
    {
        public static global::Example.Support.Person CreateValidPerson()
        {
            return global::Example.Support.PersonFixtureFactory.Valid.Create();
        }

        public static global::Example.Support.Person CreateCustomizedPerson()
        {
            return global::Example.Support.PersonFixtureFactory.Valid.Create(
                person => person with { Name = ""customized"" });
        }
    }
}";
}
