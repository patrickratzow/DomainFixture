using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.Generation.Metadata;
using DomainFixture.SourceGenerator;
using DomainFixture.Validation;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class ExternalOperationManifestIntegrationTests
{
    [Test]
    public void Generator_ShouldInvokeExternalParseBoundary_FromMetadataOnlyAdapter()
    {
        var externalAssembly = CompileReference(ExternalAdapterSource);
        var compilation = CSharpCompilation.Create(
            $"ExternalOperationConsumer_{Guid.NewGuid():N}",
            new[]
            {
                CSharpSyntaxTree.ParseText(
                    ConsumerSource,
                    new CSharpParseOptions(LanguageVersion.CSharp10))
            },
            CreateReferences().Concat(new[] { externalAssembly }),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out _);

        var result = driver.GetRunResult();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();

        var generatedTestSource = result.GeneratedTrees.Single(tree =>
            tree.FilePath.EndsWith("ExternalValueFixture.Validation.g.cs")).ToString();
        generatedTestSource.Should()
            .Contain("Assert.Throws<ArgumentException>")
            .And.Contain("ValueName.Parse(new string ('a', 2))")
            .And.NotContain("new global::External.Adapter")
            .And.NotContain("Adapter()");

        var factorySource = result.GeneratedTrees.Single(tree =>
            tree.FilePath.EndsWith("ExternalValueFixture.Factory.g.cs")).ToString();
        factorySource.Should()
            .Contain("public static class ExternalValueFixtureFactory")
            .And.Contain("public static class Validation")
            .And.Contain("return ExternalValueFixture.Baseline();");
    }

    private static MetadataReference CompileReference(string source)
    {
        var compilation = CSharpCompilation.Create(
            $"ExternalOperationAdapter_{Guid.NewGuid():N}",
            new[]
            {
                CSharpSyntaxTree.ParseText(
                    source,
                    new CSharpParseOptions(LanguageVersion.CSharp10))
            },
            CreateReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static IEnumerable<MetadataReference> CreateReferences()
    {
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(IFixtureValidator<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(DomainOperationContract).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(DomainOperationManifestAttribute).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(TestAttribute).Assembly.Location));
        return references;
    }

    private const string ExternalAdapterSource = @"
using System;
using DomainFixture.Generation.Metadata;

[assembly: DomainContractManifest(
    1,
    typeof(External.Adapter),
    typeof(External.ValueName),
    ""Value"",
    ""domainfixture.text.minimum-length"",
    new[] { ""minimum=3"" },
    ""VALUE_TOO_SHORT"",
    true)]

[assembly: DomainOperationManifest(
    2,
    typeof(External.Adapter),
    ""domainfixture.operation.static-factory:global::External.ValueName.Parse(string)"",
    ""domainfixture.operation.static-factory"",
    typeof(External.ValueName),
    typeof(External.ValueName),
    ""Parse"",
    typeof(External.ValueName),
    new[] { ""value"" },
    new[] { typeof(string) },
    new[] { ""Value"" })]

[assembly: DomainOperationOutcomeManifest(
    1,
    typeof(External.Adapter),
    typeof(External.ValueName),
    ""domainfixture.operation.static-factory:global::External.ValueName.Parse(string)"",
    ""domainfixture.operation-outcome.throws-exception"",
    new[] { ""global::System.ArgumentException"" })]

namespace External
{
    public sealed class ValueName
    {
        private ValueName(string value) => Value = value;

        public string Value { get; set; }

        public static ValueName Parse(string value)
        {
            if (value.Length < 3)
                throw new ArgumentException(""Value must contain at least three characters."", nameof(value));

            return new ValueName(value);
        }
    }

    public sealed class Adapter
    {
        private Adapter() => throw new InvalidOperationException(""Metadata adapters must not be instantiated."");
    }
}";

    private const string ConsumerSource = @"
using DomainFixture.Generation;
using DomainFixture.Validation;
using External;

namespace Consumer
{
    public sealed class ExternalValueValidator : IFixtureValidator<ValueName>
    {
        public ValidationReport Validate(ValueName subject) =>
            subject.Value.Length >= 3
                ? ValidationReport.Valid
                : new ValidationReport(new[]
                {
                    new ValidationFailure(""Value"", ""VALUE_TOO_SHORT"")
                });
    }

    public sealed class ExternalValueFixture : IFixtureTestConfiguration<ValueName>
    {
        public void Configure(IFixtureTestBuilder<ValueName> fixture)
        {
            fixture.Recipe(""Validation"")
                .Baseline(Baseline)
                .ValidateWith(Validator)
                .RulesFrom<Adapter>();
        }

        public static ValueName Baseline() => ValueName.Parse(""valid"");
        public static ExternalValueValidator Validator() => new();
    }
}";
}
