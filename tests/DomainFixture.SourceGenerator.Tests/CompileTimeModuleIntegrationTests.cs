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
public sealed class CompileTimeModuleIntegrationTests
{
    [Test]
    public void Generator_ShouldConsumeExternalModuleRecipe_WithoutLoadingModuleCode()
    {
        var producerAssembly = CompileReference(ModuleProducerSource);
        var compilation = CSharpCompilation.Create(
            $"ModuleConsumer_{Guid.NewGuid():N}",
            new[]
            {
                CSharpSyntaxTree.ParseText(
                    @"using DomainFixture.Generation;
namespace Consumer
{
    public sealed class Profile : IFixtureGenerationProfile
    {
        public void Configure(IFixtureGenerationOptions options) =>
            options.Conventions().UseImmutableObjects();
    }
}",
                    new CSharpParseOptions(LanguageVersion.CSharp10))
            },
            CreateReferences().Concat(new[] { producerAssembly }),
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

        var factory = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
            "External.Generated.PlaceOrderFixture.Factory.g.cs")).ToString();
        factory.Should()
            .Contain("public static class PlaceOrderFixtureFactory")
            .And.Contain("public static class Valid")
            .And.Contain("OrderId.From(Guid.NewGuid())")
            .And.Contain("DomainFixtureUniqueValue.NextString")
            .And.NotContain("new RequestModule")
            .And.NotContain("RequestModule()");

        result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
                "DomainFixture.ModuleCatalog.g.cs")).ToString().Should()
            .Contain("MODULE example.requests")
            .And.Contain("RECIPE global::External.PlaceOrder")
            .And.Contain("VALIDATION global::External.PlaceOrder")
            .And.Contain("ValidationContributionCount = 1");

        var tests = result.GeneratedTrees.Single(tree =>
            tree.FilePath.EndsWith("PlaceOrderFixture.Valid.g.cs")).ToString();
        tests.Should()
            .Contain("new PlaceOrderValidationAdapter()")
            .And.Contain("Valid_Description_LengthBelowMinimum_IsInvalid")
            .And.NotContain("FluentValidation");
    }

    [Test]
    public void Generator_ShouldRejectContributionWithoutDeclaredCapability()
    {
        var producerAssembly = CompileReference(ModuleProducerSource.Replace(
            "DomainFixtureModuleCapabilities.Recipes",
            "DomainFixtureModuleCapabilities.Constraints"));
        var compilation = CSharpCompilation.Create(
            $"InvalidModuleConsumer_{Guid.NewGuid():N}",
            new[] { CSharpSyntaxTree.ParseText("public sealed class TestMarker { }") },
            CreateReferences().Concat(new[] { producerAssembly }),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        driver.GetRunResult().Diagnostics.Should().ContainSingle(diagnostic =>
            diagnostic.Id == "DFG047" &&
            diagnostic.GetMessage(null).Contains("recipes capability"));
    }

    private static MetadataReference CompileReference(string source)
    {
        var compilation = CSharpCompilation.Create(
            $"ExternalModuleProducer_{Guid.NewGuid():N}",
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
        references.Add(MetadataReference.CreateFromFile(typeof(DomainFixtureModuleManifestAttribute).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(TestAttribute).Assembly.Location));
        return references;
    }

    private const string ModuleProducerSource = @"
using System;
using DomainFixture.Contracts;
using DomainFixture.Generation.Metadata;

[assembly: DomainFixtureModuleManifest(
    1,
    ""example.requests"",
    ""1.0.0"",
    typeof(External.RequestModule),
    new[]
    {
        DomainFixtureModuleCapabilities.Recipes,
        DomainFixtureModuleCapabilities.Validators,
        DomainFixtureModuleCapabilities.Constraints
    })]

[assembly: DomainFixtureRecipeManifest(
    1,
    ""example.requests"",
    typeof(External.RequestModule),
    typeof(External.PlaceOrder),
    ""External.Generated"",
    ""PlaceOrderFixture"",
    ""Valid"")]

[assembly: DomainFixtureValidationManifest(
    1,
    ""example.requests"",
    typeof(External.RequestModule),
    typeof(External.PlaceOrder),
    ""global::External.PlaceOrderValidationAdapter"")]

[assembly: DomainContractManifest(
    1,
    typeof(External.RequestModule),
    typeof(External.PlaceOrder),
    ""Description"",
    ""domainfixture.text.minimum-length"",
    new[] { ""minimum=3"" },
    ""DESCRIPTION_TOO_SHORT"",
    true)]

namespace External
{
    public readonly record struct OrderId(Guid Value)
    {
        public static OrderId From(Guid value) => new(value);
    }

    public sealed record PlaceOrder(OrderId OrderId, string Description);

    public sealed class PlaceOrderValidationAdapter : DomainFixture.Validation.IFixtureValidator<PlaceOrder>
    {
        public DomainFixture.Validation.ValidationReport Validate(PlaceOrder subject) =>
            subject.Description.Length >= 3
                ? DomainFixture.Validation.ValidationReport.Valid
                : new DomainFixture.Validation.ValidationReport(new[]
                {
                    new DomainFixture.Validation.ValidationFailure(
                        ""Description"",
                        ""DESCRIPTION_TOO_SHORT"")
                });
    }

    public sealed class RequestModule
    {
        private RequestModule() =>
            throw new InvalidOperationException(""Compile-time modules must not be instantiated."");
    }
}";
}
