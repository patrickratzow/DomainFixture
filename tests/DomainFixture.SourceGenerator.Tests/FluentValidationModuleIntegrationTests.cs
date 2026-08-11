using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.Generation.Metadata;
using DomainFixture.Modules.FluentValidation;
using DomainFixture.SourceGenerator;
using DomainFixture.Validation;
using FluentAssertions;
using FluentValidation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class FluentValidationModuleIntegrationTests
{
    [Test]
    public void Module_ShouldCompileProducerMetadata_ConsumedByCoreGenerator()
    {
        var producerCompilation = CSharpCompilation.Create(
            $"FluentProducer_{Guid.NewGuid():N}",
            new[]
            {
                CSharpSyntaxTree.ParseText(
                    ProducerSource,
                    new CSharpParseOptions(LanguageVersion.CSharp10))
            },
            CreateReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver moduleDriver = CSharpGeneratorDriver.Create(
            new FluentValidationDomainFixtureModuleGenerator().AsSourceGenerator());

        moduleDriver = moduleDriver.RunGeneratorsAndUpdateCompilation(
            producerCompilation,
            out var producerOutput,
            out _);

        var moduleResult = moduleDriver.GetRunResult();
        moduleResult.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
        var moduleSource = moduleResult.GeneratedTrees.Single().ToString();
        moduleSource.Should()
            .Contain("DomainFixtureModuleManifestAttribute")
            .And.Contain("DomainFixtureValidationManifestAttribute")
            .And.Contain("DomainContractManifestAttribute")
            .And.Contain("domainfixture.text.minimum-length")
            .And.Contain("domainfixture.int32.inclusive-range")
            .And.Contain("IFixtureValidator<global::Producer.CreateUser>");

        using var producerStream = new MemoryStream();
        var producerEmit = producerOutput.Emit(producerStream);
        producerEmit.Success.Should().BeTrue(string.Join(Environment.NewLine, producerEmit.Diagnostics));
        var producerReference = MetadataReference.CreateFromImage(producerStream.ToArray());

        var consumerCompilation = CSharpCompilation.Create(
            $"FluentConsumer_{Guid.NewGuid():N}",
            new[]
            {
                CSharpSyntaxTree.ParseText(
                    ConsumerSource,
                    new CSharpParseOptions(LanguageVersion.CSharp10))
            },
            CreateReferences().Concat(new[] { producerReference }),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver coreDriver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());

        coreDriver = coreDriver.RunGeneratorsAndUpdateCompilation(
            consumerCompilation,
            out var consumerOutput,
            out _);

        var coreResult = coreDriver.GetRunResult();
        coreResult.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
        consumerOutput.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
        coreResult.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
                "DomainFixture.ModuleCatalog.g.cs")).ToString().Should()
            .Contain("MODULE DomainFixture.Modules.FluentValidation")
            .And.Contain("domainfixture.module.validators");
        var generatedTests = coreResult.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
            "CreateUserFixture.Valid.g.cs")).ToString();
        generatedTests.Should()
            .Contain("Name_LengthBelowMinimum_IsInvalid")
            .And.Contain("Age_BelowMinimum_IsInvalid")
            .And.Contain("CreateUserCreateUserValidatorAdapter_")
            .And.NotContain("CreateUserValidFluentValidationAdapter")
            .And.NotContain("ValidationContext<");
    }

    private static IEnumerable<MetadataReference> CreateReferences()
    {
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(AbstractValidator<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(IFixtureValidator<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(DomainOperationContract).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(DomainFixtureModuleManifestAttribute).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(TestAttribute).Assembly.Location));
        return references;
    }

    private const string ProducerSource = @"
using FluentValidation;

namespace Producer
{
    public sealed record CreateUser(string Name, int Age);

    public sealed class CreateUserValidator : AbstractValidator<CreateUser>
    {
        public CreateUserValidator()
        {
            RuleFor(user => user.Name)
                .NotEmpty()
                .MinimumLength(3)
                .WithErrorCode(""NAME_SHORT"");
            RuleFor(user => user.Age)
                .InclusiveBetween(18, 120)
                .WithErrorCode(""AGE_RANGE"");
        }
    }
}";

    private const string ConsumerSource = @"
using DomainFixture.Generation;
using Producer;

namespace Consumer
{
    public sealed class Profile : IFixtureGenerationProfile
    {
        public void Configure(IFixtureGenerationOptions options) =>
            options.Conventions().UseImmutableObjects();
    }

    public sealed class CreateUserFixture : IFixtureTestConfiguration<CreateUser>
    {
        public void Configure(IFixtureTestBuilder<CreateUser> fixture) =>
            fixture.Recipe(""Valid"").Baseline(Baseline);

        public static CreateUser Baseline() => new(""alice"", 30);
    }
}";
}
