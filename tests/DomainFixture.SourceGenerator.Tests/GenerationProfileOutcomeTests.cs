using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.Generation;
using DomainFixture.SourceGenerator.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class GenerationProfileOutcomeTests
{
    [Test]
    public void ResolveOperationRejection_ShouldPreferSubjectDeclarationOverAssemblyDefault()
    {
        var profile = CreateProfile(
            new OperationRejectionSpec(null, "global::Example.DefaultException", null),
            new OperationRejectionSpec(
                "global::Example.Username",
                "global::Example.UsernameException",
                null));

        profile.ResolveOperationRejection("global::Example.Username")!.ExceptionTypeName
            .Should().Be("global::Example.UsernameException");
        profile.ResolveOperationRejection("global::Example.QualifiedName")!.ExceptionTypeName
            .Should().Be("global::Example.DefaultException");
    }

    [Test]
    public void ResolveOperationRejection_ShouldReturnNullWithoutConfiguration()
    {
        GenerationProfileSpec.Default.ResolveOperationRejection("global::Example.Username")
            .Should().BeNull();
    }

    [TestCase(@"
options.Operations()
    .RejectWith<InvalidOperationException>()
    .RejectWith<ArgumentException>();")]
    [TestCase(@"
options.Operations()
    .RejectWith<Subject, InvalidOperationException>()
    .RejectWith<Subject, ArgumentException>();")]
    public void DuplicateRejectionScope_ShouldReportDfg032(string configuration)
    {
        var source = $@"
using System;
using DomainFixture.Generation;

public sealed class Subject {{ }}

public sealed class Profile : IFixtureGenerationProfile
{{
    public void Configure(IFixtureGenerationOptions options)
    {{
        {configuration}
    }}
}}";

        var result = RunGenerator(source);

        result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "DFG032");
    }

    [Test]
    public void DifferentRejectionScopes_ShouldNotConflict()
    {
        const string source = @"
using System;
using DomainFixture.Generation;

public sealed class Subject { }

public sealed class Profile : IFixtureGenerationProfile
{
    public void Configure(IFixtureGenerationOptions options)
    {
        options.Operations()
            .RejectWith<InvalidOperationException>()
            .RejectWith<Subject, ArgumentException>();
    }
}";

        RunGenerator(source).Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "DFG032");
    }

    [Test]
    public void GenericResultConfiguration_ShouldResolveBySubjectAndWrapper()
    {
        var configured = new OperationResultSpec(
            "global::Example.Subject",
            "global::Example.Result<global::Example.Subject>",
            "IsSuccess",
            "Value",
            null);
        var profile = new GenerationProfileSpec(
            false, false, false, false, false,
            FixtureActivationKind.Factories,
            null,
            ImmutableArray<PropertyMutationSpec>.Empty,
            ImmutableArray<OperationRejectionSpec>.Empty,
            ImmutableArray.Create(configured));

        profile.ResolveOperationResult(
                configured.SubjectTypeName,
                configured.ResultTypeName)
            .Should().BeSameAs(configured);
    }

    [Test]
    public void DuplicateGenericResultConfiguration_ShouldReportDfg043()
    {
        const string source = @"
using DomainFixture.Generation;

public sealed class Subject { }
public sealed class Result<T>
{
    public bool IsSuccess { get; set; }
    public T Value { get; set; } = default!;
}

public sealed class Profile : IFixtureGenerationProfile
{
    public void Configure(IFixtureGenerationOptions options)
    {
        options.Operations()
            .UseResult<Subject, Result<Subject>>(x => x.IsSuccess, x => x.Value)
            .UseResult<Subject, Result<Subject>>(x => x.IsSuccess, x => x.Value);
    }
}";

        RunGenerator(source).Diagnostics.Should()
            .ContainSingle(diagnostic => diagnostic.Id == "DFG043");
    }

    [Test]
    public void DuplicateConfiguredValue_ShouldReportDfg045()
    {
        const string source = @"
using DomainFixture.Generation;

public enum Status { Pending, Approved }

public sealed class Profile : IFixtureGenerationProfile
{
    public void Configure(IFixtureGenerationOptions options)
    {
        options.Values()
            .For<Status>(() => Status.Pending)
            .For<Status>(() => Status.Approved);
    }
}";

        RunGenerator(source).Diagnostics.Should()
            .ContainSingle(diagnostic => diagnostic.Id == "DFG045");
    }

    [Test]
    public void InstanceDependentConfiguredValue_ShouldReportDfg044()
    {
        const string source = @"
using DomainFixture.Generation;

public sealed class Profile : IFixtureGenerationProfile
{
    public void Configure(IFixtureGenerationOptions options)
    {
        options.Values().For<string>(() => CreateValue());
    }

    private string CreateValue() => ""runtime"";
}";

        RunGenerator(source).Diagnostics.Should()
            .ContainSingle(diagnostic => diagnostic.Id == "DFG044");
    }

    [Test]
    public void StaticFactoryConfiguredValue_ShouldBeAccepted()
    {
        const string source = @"
using DomainFixture.Generation;

public sealed class Currency
{
    private Currency(string code) { }
    public static Currency From(string code) => new Currency(code);
}

public sealed class Profile : IFixtureGenerationProfile
{
    public void Configure(IFixtureGenerationOptions options)
    {
        options.Values().For<Currency>(() => Currency.From(""EUR""));
    }
}";

        RunGenerator(source).Diagnostics.Should()
            .NotContain(diagnostic =>
                diagnostic.Id == "DFG044" || diagnostic.Id == "DFG045");
    }

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

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = CSharpCompilation.Create(
            $"ProfileOutcome_{Guid.NewGuid():N}",
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp10)) },
            CreateReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    private static IEnumerable<MetadataReference> CreateReferences()
    {
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(IFixtureGenerationProfile).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(DomainOperationContract).Assembly.Location));
        return references;
    }
}
