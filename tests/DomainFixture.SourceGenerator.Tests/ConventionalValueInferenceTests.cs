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
public sealed class ConventionalValueInferenceTests
{
    [Test]
    public void Generator_ShouldRecursivelyInferStrongIdsAndUniqueValueFactories()
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
            $"ConventionalValueInference_{Guid.NewGuid():N}",
            new[]
            {
                CSharpSyntaxTree.ParseText(
                    Source,
                    new CSharpParseOptions(LanguageVersion.CSharp10))
            },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        diagnostics.Should().BeEmpty();
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
        var factory = driver.GetRunResult().Results
            .SelectMany(result => result.GeneratedSources)
            .Single(source => source.HintName.EndsWith("PurchaseFixture.Factory.g.cs"))
            .SourceText
            .ToString();
        factory.Should().Contain(
            "OrderId.From(Guid.NewGuid())");
        factory.Should().Contain("Money.Usd(1M)");
        factory.Should().Contain("Purchase.Create");
    }

    private const string Source = @"
using System;
using DomainFixture.Generation;

namespace Example
{
    public readonly record struct OrderId
    {
        private OrderId(Guid value) => Value = value;
        public Guid Value { get; }
        public static OrderId From(Guid value) => new OrderId(value);
    }

    public readonly record struct Money
    {
        private Money(decimal amount) => Amount = amount;
        public decimal Amount { get; }
        public static Money Usd(decimal amount) => new Money(amount);
    }

    public sealed class Purchase
    {
        private Purchase(OrderId id, Money total)
        {
            Id = id;
            Total = total;
        }

        public OrderId Id { get; }
        public Money Total { get; }
        public static Purchase Create(OrderId id, Money total) => new Purchase(id, total);
    }

    public sealed class PurchaseFixture : IFixtureTestConfiguration<Purchase>
    {
        public void Configure(IFixtureTestBuilder<Purchase> fixture)
        {
            fixture.Recipe(""Valid"").Synthesize();
        }
    }
}";
}
