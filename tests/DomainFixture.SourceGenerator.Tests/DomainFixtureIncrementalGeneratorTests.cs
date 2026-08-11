using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DomainFixture.Contracts;
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
        result.GeneratedTrees.Should().HaveCount(6);
        result.GeneratedTrees.Should().Contain(tree => tree.FilePath.EndsWith(
            "DomainFixture.DomainSpecSnapshot.g.cs"));
        result.GeneratedTrees.Should().Contain(tree => tree.FilePath.EndsWith(
            "DomainFixture.DomainCoverageReport.g.cs"));
        result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
                "DomainFixture.ValidationRuleManifest.g.cs")).ToString().Should()
            .Contain("DomainContractManifestAttribute")
            .And.Contain("domainfixture.text.length");
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
            ".Baseline(Baseline)\n                .ValidateWith(Validator)",
            ".ValidateWith(Validator)");

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
        result.GeneratedTrees.Should().HaveCount(7);
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
        result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
                "ExternalUserFixtureConfiguration.Registration.g.cs")).ToString().Should()
            .Contain("Registration_Description_LengthBelowMinimum_IsInvalid")
            .And.Contain("DESCRIPTION_LENGTH");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldConsumeGenericDomainContract_FromExternalAdapter()
    {
        var contractAssembly = CompileReference(ExternalDomainContractSource);

        var result = RunGenerator(MetadataConsumerSource, out var outputCompilation, contractAssembly);

        result.Diagnostics.Should().BeEmpty();
        result.GeneratedTrees.Should().ContainSingle(tree => tree.FilePath.EndsWith(
            "ExternalUserFixtureConfiguration.Registration.g.cs"));
        result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
                "ExternalUserFixtureConfiguration.Registration.g.cs")).ToString().Should()
            .Contain("Registration_Description_LengthBelowMinimum_IsInvalid")
            .And.Contain("DESCRIPTION_LENGTH");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldRejectUnsupportedDomainContractSchema()
    {
        var contractAssembly = CompileReference(ExternalDomainContractSource.Replace(
            "    1,\n    typeof(External.RegistrationRules)",
            "    2,\n    typeof(External.RegistrationRules)"));

        var result = RunGenerator(MetadataConsumerSource, out _, contractAssembly);

        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "DFG018");
        result.GeneratedTrees.Should().ContainSingle(tree => tree.FilePath.EndsWith(
            "ExternalUserFixtureConfiguration.Factory.g.cs"));
    }

    [Test]
    public void Generator_ShouldReportGenericContractsWithoutBoundaryProviders()
    {
        var contractAssembly = CompileReference(ExternalDomainContractSource.Replace(
            "domainfixture.text.length",
            "example.custom.business-rule"));

        var result = RunGenerator(MetadataConsumerSource, out _, contractAssembly);

        result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "DFG017");
        result.GeneratedTrees.Should().ContainSingle(tree => tree.FilePath.EndsWith(
            "ExternalUserFixtureConfiguration.Factory.g.cs"));
    }

    [Test]
    public void Generator_ShouldGenerateScenarioOnlySuite_FromExternalScenarioManifest()
    {
        var scenarioAssembly = CompileReference(ExternalScenarioSource);

        var result = RunGenerator(ScenarioConsumerSource, out var outputCompilation, scenarioAssembly);

        result.Diagnostics.Should().BeEmpty();
        var source = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
            "ValueNameFixture.Laws.g.cs")).ToString();
        source.Should()
            .Contain("Equality_IsReflexive")
            .And.Contain("Equality_EquivalentValuesAreSymmetric")
            .And.Contain("Equality_EquivalentValuesHaveSameHashCode")
            .And.Contain("Construction_Constructor_Value_BaselineSucceeds")
            .And.Contain("Construction_Constructor_Value_ArgumentsRoundTrip")
            .And.NotContain("validator");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldGenerateConstructionSuite_FromConventionalMultiPropertyFactory()
    {
        var result = RunGenerator(ConstructionOnlySource, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        var source = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
            "QualifiedNameFixture.Construction.g.cs")).ToString();
        source.Should()
            .Contain("Construction_From_Name_Realm_BaselineSucceeds")
            .And.Contain("Construction_From_Name_Realm_ArgumentsRoundTrip")
            .And.Contain("QualifiedName.From(baseline.Name, baseline.Realm)")
            .And.Contain("constructed.Name")
            .And.Contain("constructed.Realm")
            .And.NotContain("validator");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldReportDiagnostic_WhenOperationParameterMappingIsAmbiguous()
    {
        var source = ConstructionOnlySource.Replace(
            "string name, string realm) => new(name, realm)",
            "string first, string second) => new(first, second)");

        var result = RunGenerator(source, out _);

        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "DFG025");
        result.GeneratedTrees.Should().ContainSingle(tree => tree.FilePath.EndsWith(
            "QualifiedNameFixture.Factory.g.cs"));
    }

    [Test]
    public void Generator_ShouldReportDiagnostic_WhenManifestPropertyCannotBeAssigned()
    {
        var rulesAssembly = CompileReference(ExternalRulesSource
            .Replace("true)]", "false)]")
            .Replace("get; set;", "get; private set;"));

        var result = RunGenerator(
            MetadataConsumerSource.Replace(
                "new() { Description = \"baseline\" }",
                "new()"),
            out _,
            rulesAssembly);

        result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "DFG005");
        result.GeneratedTrees.Should().ContainSingle(tree => tree.FilePath.EndsWith(
            "ExternalUserFixtureConfiguration.Factory.g.cs"));
    }

    [Test]
    public void Generator_ShouldUseRecordWith_ForSealedRecords()
    {
        var source = WithImmutableObjects(ValidSource)
            .Replace(
                @"public sealed class User
    {
        public string Description { get; set; } = string.Empty;
    }",
                @"public sealed record User(string Description, string Realm);")
            .Replace(
                "new() { Description = \"baseline\" }",
                "new(\"baseline\", \"realm\")");

        var result = RunGenerator(source, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
                "UserFixtureConfiguration.Registration.g.cs")).ToString().Should()
            .Contain("return source with {Description = value};")
            .And.NotContain("subject.Description =");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldUseConstructorReconstruction_ForSealedConstructorOnlyTypes()
    {
        var source = WithImmutableObjects(ValidSource)
            .Replace(
                @"public sealed class User
    {
        public string Description { get; set; } = string.Empty;
    }",
                @"public sealed class User
    {
        public User(string description, string realm)
        {
            Description = description;
            Realm = realm;
        }

        public string Description { get; }
        public string Realm { get; }
    }")
            .Replace(
                "new() { Description = \"baseline\" }",
                "new(\"baseline\", \"realm\")");

        var result = RunGenerator(source, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
                "UserFixtureConfiguration.Registration.g.cs")).ToString().Should()
            .Contain("return new User(value, source.Realm);");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldReportEveryUnsupportedBoundaryRule()
    {
        var source = WithImmutableObjects(ValidSource)
            .Replace(
                @"public sealed class User
    {
        public string Description { get; set; } = string.Empty;
    }",
                @"public sealed class User
    {
        private User(string description) => Description = description;

        public string Description { get; }

        public static User From(string description) => new(description);
    }")
            .Replace(
                "new() { Description = \"baseline\" }",
                "User.From(\"baseline\")");

        var result = RunGenerator(source, out _);

        result.Diagnostics.Where(diagnostic => diagnostic.Id == "DFG005")
            .Should().HaveCount(3)
            .And.OnlyContain(diagnostic =>
                diagnostic.GetMessage(null).Contains("Consumer.User.Description") &&
                diagnostic.GetMessage(null).Contains("no usable record 'with'"));
        result.GeneratedTrees.Should().ContainSingle(tree => tree.FilePath.EndsWith(
            "DomainFixture.ValidationRuleManifest.g.cs"));
    }

    [Test]
    public void Generator_ShouldReportRecognizedRulesWithoutConstraintAdapters()
    {
        var source = ValidSource
            .Replace(
                "public static RuleBuilder<T, TProperty> WithErrorCode<T, TProperty>",
                @"public static RuleBuilder<T, string> EmailAddress<T>(
            this RuleBuilder<T, string> builder) => builder;

        public static RuleBuilder<T, TProperty> WithErrorCode<T, TProperty>")
            .Replace(
                ".NotEmpty()\n                .WithErrorCode(\"DESCRIPTION_NOT_EMPTY\")",
                ".NotEmpty()\n                .WithErrorCode(\"DESCRIPTION_NOT_EMPTY\")\n                .EmailAddress()");

        var result = RunGenerator(source, out var outputCompilation);

        result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "DFG015");
        result.GeneratedTrees.Should().Contain(tree => tree.FilePath.EndsWith(
            "UserFixtureConfiguration.Registration.g.cs"));
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
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

    [Test]
    public void Generator_ShouldSynthesizeValidFactory_WhenRecipeRequestsIt()
    {
        var result = RunGenerator(SynthesizedRecipeSource, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        var factory = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
            "AccountFixture.Factory.g.cs")).ToString();
        factory.Should().Contain("Account.From(DomainFixtureUniqueValue.NextString(1, 2147483647), 1)");
        result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
                "DomainFixture.DomainCoverageReport.g.cs")).ToString().Should()
            .Contain("[Both] Operation");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldSynthesizeSourceLessRecipe_WhenProfileEnablesConvention()
    {
        var source = SynthesizedRecipeSource.Replace(".Synthesize();", ";");
        source = source.Insert(
            source.IndexOf("    public sealed class Account", StringComparison.Ordinal),
            @"    public sealed class Profile : IFixtureGenerationProfile
    {
        public void Configure(IFixtureGenerationOptions options)
        {
            options.Conventions().AutoSynthesizeRecipes();
        }
    }

");

        var result = RunGenerator(source, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
                "AccountFixture.Factory.g.cs")).ToString().Should()
            .Contain("Account.From(DomainFixtureUniqueValue.NextString(1, 2147483647), 1)");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldUnwrapGenericResultFactory_AndTestFailureBoundaries()
    {
        var result = RunGenerator(GenericResultRecipeSource, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        var factory = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
            "NameFixture.Factory.g.cs")).ToString();
        factory.Should()
            .Contain("Func<Name>")
            .And.Contain("result.IsSuccess")
            .And.Contain("return result.Value");
        var suite = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
            "NameFixture.Valid.g.cs")).ToString();
        suite.Should()
            .Contain("result.IsSuccess, Is.True")
            .And.Contain("result.Value")
            .And.Contain("ReturnsFailure")
            .And.Contain("Is.False");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldComposeNestedFactories_Collections_AndConfiguredCommandValues()
    {
        var result = RunGenerator(NestedSynthesisSource, out var outputCompilation);

        result.Diagnostics.Should().BeEmpty();
        var factory = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
            "OrderFixture.Factory.g.cs")).ToString();
        factory.Should()
            .Contain("AddressFixtureFactory.Valid.Create()")
            .And.Contain("Currency.Eur")
            .And.Contain("new string[]")
            .And.Contain("DomainFixtureUniqueValue.NextString(1, 2147483647)")
            .And.Contain("new Dictionary<string, Address>")
            .And.Contain("[DomainFixtureUniqueValue.NextString(1, 2147483647)] = AddressFixtureFactory.Valid.Create()");

        var suite = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith(
            "OrderFixture.Valid.g.cs")).ToString();
        suite.Should().Contain("subject.WithCurrency(Currency.Eur)")
            .And.Contain("subject.WithAddress(AddressFixtureFactory.Valid.Create())");
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Test]
    public void Generator_ShouldReportNestedConstructionCycles_WithoutEmittingBrokenFactories()
    {
        var result = RunGenerator(CyclicSynthesisSource, out _);

        result.Diagnostics.Where(diagnostic => diagnostic.Id == "DFG042")
            .Should().HaveCount(2)
            .And.OnlyContain(diagnostic => diagnostic.GetMessage(null).Contains(
                "nested construction cycle"));
        result.GeneratedTrees.Should().NotContain(tree =>
            tree.FilePath.EndsWith("ARecordFixture.Factory.g.cs") ||
            tree.FilePath.EndsWith("BRecordFixture.Factory.g.cs"));
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
        references.Add(MetadataReference.CreateFromFile(typeof(DomainConstraintContract).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(TestAttribute).Assembly.Location));

        return references;
    }

    private static string WithImmutableObjects(string source) => source.Replace(
        ".UsePropertyNames();",
        ".UsePropertyNames()\n                .UseImmutableObjects();");

    private const string SynthesizedRecipeSource = @"
using DomainFixture.Generation;

namespace Consumer
{
    public sealed class Account
    {
        private Account(string name, int level)
        {
            Name = name;
            Level = level;
        }

        public string Name { get; }
        public int Level { get; }

        public static Account From(string name, int level) => new(name, level);
    }

    public sealed class AccountFixture : IFixtureTestConfiguration<Account>
    {
        public void Configure(IFixtureTestBuilder<Account> fixture)
        {
            fixture.Recipe(""Valid"")
                .Synthesize();
        }
    }
}";

    private const string NestedSynthesisSource = @"
using System.Collections.Generic;
using DomainFixture.Generation;

namespace Consumer
{
    public enum Currency { Unknown, Eur }

    public sealed class Address
    {
        private Address(string city) => City = city;
        public string City { get; }
        public static Address Create(string city) => new(city);
    }

    public sealed class Order
    {
        private Order(
            Address address,
            Currency currency,
            IReadOnlyList<string> tags,
            IReadOnlyDictionary<string, Address> addresses)
        {
            Address = address;
            Currency = currency;
            Tags = tags;
            Addresses = addresses;
        }

        public Address Address { get; }
        public Currency Currency { get; }
        public IReadOnlyList<string> Tags { get; }
        public IReadOnlyDictionary<string, Address> Addresses { get; }

        public static Order Create(
            Address address,
            Currency currency,
            IReadOnlyList<string> tags,
            IReadOnlyDictionary<string, Address> addresses) =>
            new(address, currency, tags, addresses);

        public Order WithCurrency(Currency currency) =>
            new(Address, currency, Tags, Addresses);

        public Order WithAddress(Address address) =>
            new(address, Currency, Tags, Addresses);
    }

    public sealed class Profile : IFixtureGenerationProfile
    {
        public void Configure(IFixtureGenerationOptions options)
        {
            options.Values().For<Currency>(() => Currency.Eur);
        }
    }

    public sealed class AddressFixture : IFixtureTestConfiguration<Address>
    {
        public void Configure(IFixtureTestBuilder<Address> fixture)
        {
            fixture.Recipe(""Valid"").Synthesize();
        }
    }

    public sealed class OrderFixture : IFixtureTestConfiguration<Order>
    {
        public void Configure(IFixtureTestBuilder<Order> fixture)
        {
            fixture.Recipe(""Valid"")
                .Synthesize()
                .Transition(
                    ""Use EUR"",
                    subject => subject.WithCurrency(FixtureValue.Auto<Currency>()),
                    subject => subject.Currency,
                    Currency.Eur)
                .Transition(
                    ""Replace address"",
                    subject => subject.WithAddress(FixtureValue.Auto<Address>()),
                    subject => subject.Address.City,
                    ""a"");
        }
    }
}";

    private const string CyclicSynthesisSource = @"
using DomainFixture.Generation;

namespace Consumer
{
    public sealed class ARecord
    {
        private ARecord(BRecord child) => Child = child;
        public BRecord Child { get; }
        public static ARecord Create(BRecord child) => new(child);
    }

    public sealed class BRecord
    {
        private BRecord(ARecord parent) => Parent = parent;
        public ARecord Parent { get; }
        public static BRecord Create(ARecord parent) => new(parent);
    }

    public sealed class ARecordFixture : IFixtureTestConfiguration<ARecord>
    {
        public void Configure(IFixtureTestBuilder<ARecord> fixture)
        {
            fixture.Recipe(""Valid"").Synthesize();
        }
    }

    public sealed class BRecordFixture : IFixtureTestConfiguration<BRecord>
    {
        public void Configure(IFixtureTestBuilder<BRecord> fixture)
        {
            fixture.Recipe(""Valid"").Synthesize();
        }
    }
}";

    private const string GenericResultRecipeSource = @"
#nullable enable
using System;
using System.Linq.Expressions;
using DomainFixture.Generation;
using DomainFixture.Validation;
using FluentValidation;

namespace FluentValidation
{
    public sealed class RuleBuilder<T, TProperty> { }
    public abstract class AbstractValidator<T>
    {
        protected RuleBuilder<T, TProperty> RuleFor<TProperty>(
            Expression<Func<T, TProperty>> expression) => new();
    }
    public static class DefaultValidatorExtensions
    {
        public static RuleBuilder<T, string> NotEmpty<T>(
            this RuleBuilder<T, string> builder) => builder;
        public static RuleBuilder<T, string> MaximumLength<T>(
            this RuleBuilder<T, string> builder,
            int maximum) => builder;
        public static RuleBuilder<T, TProperty> WithErrorCode<T, TProperty>(
            this RuleBuilder<T, TProperty> builder,
            string errorCode) => builder;
    }
}

namespace Consumer
{
    public sealed class Result<T>
    {
        private Result(bool isSuccess, T value)
        {
            IsSuccess = isSuccess;
            Value = value;
        }

        public bool IsSuccess { get; }
        public T Value { get; }
        public static Result<T> Success(T value) => new(true, value);
        public static Result<T> Failure() => new(false, default!);
    }

    public sealed class Name
    {
        private Name(string value) => Value = value;
        public string Value { get; set; }

        public static Result<Name> Create(string value) =>
            string.IsNullOrEmpty(value) || value.Length > 8
                ? Result<Name>.Failure()
                : Result<Name>.Success(new Name(value));
    }

    public sealed class NameRules : AbstractValidator<Name>
    {
        public NameRules()
        {
            RuleFor(name => name.Value)
                .NotEmpty()
                .WithErrorCode(""NAME_REQUIRED"")
                .MaximumLength(8)
                .WithErrorCode(""NAME_LENGTH"");
        }
    }

    public sealed class NameValidation : IFixtureValidator<Name>
    {
        public ValidationReport Validate(Name subject) => ValidationReport.Valid;
    }

    public sealed class ProjectProfile : IFixtureGenerationProfile
    {
        public void Configure(IFixtureGenerationOptions options)
        {
            options.Operations()
                .UseResult<Name, Result<Name>>(
                    result => result.IsSuccess,
                    result => result.Value);
        }
    }

    public sealed class NameFixture : IFixtureTestConfiguration<Name>
    {
        public void Configure(IFixtureTestBuilder<Name> fixture)
        {
            fixture.Recipe(""Valid"")
                .Synthesize()
                .ValidateWith(Validator)
                .RulesFrom<NameRules>();
        }

        public static NameValidation Validator() => new();
    }
}";

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
    typeof(External.User),
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

    private const string ExternalDomainContractSource = @"
using DomainFixture.Generation.Metadata;

[assembly: DomainContractManifest(
    1,
    typeof(External.RegistrationRules),
    typeof(External.User),
    ""Description"",
    ""domainfixture.text.length"",
    new[] { ""minimum=4"", ""maximum=8"" },
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

    private const string ExternalScenarioSource = @"
using DomainFixture.Generation.Metadata;

[assembly: DomainScenarioManifest(
    1,
    typeof(External.ValueObjectAdapter),
    typeof(External.ValueName),
    ""domainfixture.law.value-object-equality"",
    new string[0])]

namespace External
{
    public sealed record ValueName(string Value);

    public sealed class ValueObjectAdapter
    {
    }
}";

    private const string ScenarioConsumerSource = @"
using DomainFixture.Generation;
using External;

namespace Consumer
{
    public sealed class ValueNameFixture : IFixtureTestConfiguration<ValueName>
    {
        public void Configure(IFixtureTestBuilder<ValueName> fixture)
        {
            fixture.Recipe(""Laws"")
                .Baseline(Baseline);
        }

        public static ValueName Baseline() => new(""baseline"");
    }
}";

    private const string ConstructionOnlySource = @"
using DomainFixture.Generation;

namespace Consumer
{
    public sealed class QualifiedName
    {
        private QualifiedName(string name, string realm)
        {
            Name = name;
            Realm = realm;
        }

        public string Name { get; }
        public string Realm { get; }

        public static QualifiedName From(string name, string realm) => new(name, realm);
    }

    public sealed class QualifiedNameFixture : IFixtureTestConfiguration<QualifiedName>
    {
        public void Configure(IFixtureTestBuilder<QualifiedName> fixture)
        {
            fixture.Recipe(""Construction"")
                .Baseline(Baseline);
        }

        public static QualifiedName Baseline() => QualifiedName.From(""alice"", ""example"");
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
