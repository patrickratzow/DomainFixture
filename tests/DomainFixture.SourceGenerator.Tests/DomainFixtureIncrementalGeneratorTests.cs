using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DomainFixture.SourceGenerator;
using DomainFixture.Validation;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public class DomainFixtureIncrementalGeneratorTests
{
    [Test]
    public void Generator_ShouldEmitCompilableTests_FromFluentConfiguration()
    {
        var result = RunGenerator(ValidSource, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        result.GeneratedTrees.Should().HaveCount(2);
        result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
                "UserFixtureConfiguration.Registration.g.cs")).ToString().Should()
            .Contain("Registration_Description_LengthBelowMinimum_IsInvalid")
            .And.Contain("Registration_Description_NotEmpty_Empty_IsInvalid")
            .And.Contain("Registration_Description_NotNull_Null_IsInvalid")
            .And.Contain("DESCRIPTION_LENGTH");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldReportDiagnostic_WhenFluentChainIsIncomplete()
    {
        var source = ValidSource.Replace(
            ".RulesFrom<RegistrationRules>();",
            ";");

        var result = RunGenerator(source, out _);

        result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "DFG001");
        result.GeneratedTrees.Should().ContainSingle(tree => tree.FilePath.EndsWith(
            "DomainFixture.ValidationRuleManifest.g.cs"));
    }

    [Test]
    public void Generator_ShouldEmitOneSuitePerRecipe()
    {
        var source = ValidSource.Replace(
            ".RulesFrom<RegistrationRules>();",
            @".RulesFrom<RegistrationRules>();

            fixture.Recipe(""Update"")
                .Baseline(Baseline)
                .ValidateWith(Validator)
                .RulesFrom<RegistrationRules>();");

        var result = RunGenerator(source, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        result.GeneratedTrees.Should().HaveCount(3);
        result.GeneratedTrees.Select(tree => tree.FilePath).Should().Contain(path =>
            path.EndsWith("UserFixtureConfiguration.Registration.g.cs"));
        result.GeneratedTrees.Select(tree => tree.FilePath).Should().Contain(path =>
            path.EndsWith("UserFixtureConfiguration.Update.g.cs"));
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldConsumeRuleManifest_FromReferencedAssembly()
    {
        var rulesAssembly = CompileReference(ExternalRulesSource);

        var result = RunGenerator(MetadataConsumerSource, out var outputCompilation, rulesAssembly);

        result.Diagnostics.Should().BeEmpty();
        result.GeneratedTrees.Should().ContainSingle(tree => tree.FilePath.EndsWith(
            "ExternalUserFixtureConfiguration.Registration.g.cs"));
        result.GeneratedTrees.Single().ToString().Should()
            .Contain("Registration_Description_LengthBelowMinimum_IsInvalid")
            .And.Contain("DESCRIPTION_LENGTH");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldReportDiagnostic_WhenManifestPropertyCannotBeAssigned()
    {
        var rulesAssembly = CompileReference(ExternalRulesSource.Replace("true)]", "false)]"));

        var result = RunGenerator(MetadataConsumerSource, out _, rulesAssembly);

        result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "DFG005");
        result.GeneratedTrees.Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldGenerateConventionRules_WhenProviderFindsNoRules()
    {
        var source = ValidSource.Replace(ValidatorChain, ";");

        var result = RunGenerator(source, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        var generatedTest = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
            "UserFixtureConfiguration.Registration.g.cs"));
        generatedTest.ToString().Should()
            .Contain("Registration_Description_NotNull_Null_IsInvalid")
            .And.Contain("Registration_Description_NotEmpty_Empty_IsInvalid")
            .And.NotContain("LengthBelowMinimum");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldReportDiagnostic_WhenMoreThanOneProfileExists()
    {
        var source = ValidSource.Replace(
            "public sealed class ProjectProfile",
            @"public sealed class OtherProjectProfile : IFixtureGenerationProfile
    {
        public void Configure(IFixtureGenerationOptions options)
        {
        }
    }

    public sealed class ProjectProfile");

        var result = RunGenerator(source, out _);

        result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "DFG007");
        result.GeneratedTrees.Should().ContainSingle(tree => tree.FilePath.EndsWith(
            "DomainFixture.ValidationRuleManifest.g.cs"));
    }

    [Test]
    public void Generator_ShouldAcceptOptionalServiceProviderActivation()
    {
        var source = ValidSource
            .Replace(
                ".UseFactories();",
                ".UseServiceProvider<TestServiceProviderFactory>();")
            .Replace(
                "public sealed class ProjectProfile",
                @"public sealed class TestServiceProviderFactory : IFixtureServiceProviderFactory
    {
        public IServiceProvider CreateServiceProvider() => throw new NotImplementedException();
    }

    public sealed class ProjectProfile");

        var result = RunGenerator(source, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    private static GeneratorDriverRunResult RunGenerator(
        string source,
        out Compilation outputCompilation,
        params MetadataReference[] additionalReferences)
    {
        var references = CreateReferences().Concat(additionalReferences);
        var compilation = CSharpCompilation.Create(
            $"GeneratorTests_{Guid.NewGuid():N}",
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp10)) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out outputCompilation,
            out _);

        return driver.GetRunResult();
    }

    private static MetadataReference CompileReference(string source)
    {
        var compilation = CSharpCompilation.Create(
            $"ExternalRules_{Guid.NewGuid():N}",
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp10)) },
            CreateReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        emitResult.Success.Should().BeTrue(string.Join(Environment.NewLine, emitResult.Diagnostics));

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static IEnumerable<MetadataReference> CreateReferences()
    {
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(IFixtureValidator<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(TestAttribute).Assembly.Location));

        return references;
    }

    private const string ValidSource = @"
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DomainFixture.Generation;
using DomainFixture.Validation;
using FluentValidation;

namespace FluentValidation
{
    public sealed class RuleBuilder<T, TProperty>
    {
    }

    public abstract class AbstractValidator<T>
    {
        protected RuleBuilder<T, TProperty> RuleFor<TProperty>(
            Expression<Func<T, TProperty>> expression) => new();
    }

    public static class DefaultValidatorExtensions
    {
        public static RuleBuilder<T, string> Length<T>(
            this RuleBuilder<T, string> builder,
            int minimum,
            int maximum) => builder;

        public static RuleBuilder<T, string> NotEmpty<T>(
            this RuleBuilder<T, string> builder) => builder;

        public static RuleBuilder<T, string> NotNull<T>(
            this RuleBuilder<T, string> builder) => builder;

        public static RuleBuilder<T, TProperty> WithErrorCode<T, TProperty>(
            this RuleBuilder<T, TProperty> builder,
            string errorCode) => builder;
    }
}

namespace Consumer
{
    public sealed class ProjectProfile : IFixtureGenerationProfile
    {
        public void Configure(IFixtureGenerationOptions options)
        {
            options.Conventions()
                .UseNullability()
                .UsePropertyNames();

            options.Activation()
                .UseFactories();
        }
    }

    public sealed class User
    {
        public string Description { get; set; } = string.Empty;
    }

    public sealed class RegistrationRules : AbstractValidator<User>
    {
        public RegistrationRules()
        {
            RuleFor(user => user.Description)
                .NotNull()
                .WithErrorCode(""DESCRIPTION_REQUIRED"")
                .NotEmpty()
                .WithErrorCode(""DESCRIPTION_NOT_EMPTY"")
                .Length(4, 8)
                .WithErrorCode(""DESCRIPTION_LENGTH"");
        }
    }

    public sealed class RegistrationValidator : IFixtureValidator<User>
    {
        public ValidationReport Validate(User subject) => ValidationReport.Valid;
    }

    public sealed class UserFixtureConfiguration : IFixtureTestConfiguration<User>
    {
        public void Configure(IFixtureTestBuilder<User> fixture)
        {
            fixture.Recipe(""Registration"")
                .Baseline(Baseline)
                .ValidateWith(Validator)
                .RulesFrom<RegistrationRules>();
        }

        public static User Baseline() => new() { Description = ""baseline"" };
        public static RegistrationValidator Validator() => new();
    }
}";

    private const string ValidatorChain = @"
                .NotNull()
                .WithErrorCode(""DESCRIPTION_REQUIRED"")
                .NotEmpty()
                .WithErrorCode(""DESCRIPTION_NOT_EMPTY"")
                .Length(4, 8)
                .WithErrorCode(""DESCRIPTION_LENGTH"");";

    private const string ExternalRulesSource = @"
using DomainFixture.Generation.Metadata;

[assembly: ValidationRuleManifest(
    typeof(External.RegistrationRules),
    ""Description"",
    ValidationRuleManifestKind.StringLength,
    4,
    8,
    ""DESCRIPTION_LENGTH"",
    true)]

namespace External
{
    public sealed class User
    {
        public string Description { get; set; } = string.Empty;
    }

    public sealed class RegistrationRules
    {
    }
}";

    private const string MetadataConsumerSource = @"
using DomainFixture.Generation;
using DomainFixture.Validation;
using External;

namespace Consumer
{
    public sealed class ExternalUserValidation : IFixtureValidator<User>
    {
        public ValidationReport Validate(User subject) => ValidationReport.Valid;
    }

    public sealed class ExternalUserFixtureConfiguration : IFixtureTestConfiguration<User>
    {
        public void Configure(IFixtureTestBuilder<User> fixture)
        {
            fixture.Recipe(""Registration"")
                .Baseline(Baseline)
                .ValidateWith(Validator)
                .RulesFrom<RegistrationRules>();
        }

        public static User Baseline() => new() { Description = ""baseline"" };
        public static ExternalUserValidation Validator() => new();
    }
}";
}
