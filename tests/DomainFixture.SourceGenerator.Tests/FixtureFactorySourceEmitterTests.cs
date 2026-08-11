using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Emission;
using DomainFixture.SourceGenerator.Models;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class FixtureFactorySourceEmitterTests
{
    [Test]
    public void EmitSource_ShouldGenerateFreshBaselineAndTransformOverload()
    {
        var configuration = CreateConfiguration();

        var source = FixtureFactorySourceEmitter.EmitSource(
            new[] { configuration },
            _ => true);

        source.Should()
            .Contain("public static class UserFixtureFactory")
            .And.Contain("public static class Validation")
            .And.Contain("public static global::Example.User Create()")
            .And.Contain("return global::Example.UserFixture.Baseline();")
            .And.Contain(
                "public static global::Example.User Create(global::System.Func<global::Example.User, global::Example.User> transform)")
            .And.Contain("return transform(Create());")
            .And.Contain("throw new global::System.ArgumentNullException(nameof(transform));");
    }

    [Test]
    public void EmitSource_ShouldGenerateOneNestedClassPerRecipe()
    {
        var configurations = new[]
        {
            CreateConfiguration(recipeName: "Registration"),
            CreateConfiguration(recipeName: "Approved user"),
        };

        var source = FixtureFactorySourceEmitter.EmitSource(configurations, _ => true);

        source.Should()
            .Contain("public static class Registration")
            .And.Contain("public static class Approved_user");
        Count(source, "static class UserFixtureFactory").Should().Be(1);
        Count(source, "return global::Example.UserFixture.Baseline();").Should().Be(2);
    }

    [Test]
    public void EmitSource_ShouldDefaultToInternalAccessibility()
    {
        var source = FixtureFactorySourceEmitter.EmitSource(new[] { CreateConfiguration() });

        source.Should()
            .Contain("internal static class UserFixtureFactory")
            .And.Contain("internal static class Validation")
            .And.Contain("internal static global::Example.User Create()");
    }

    [TestCase("Approved user", "Approved_user")]
    [TestCase("123-ready", "_123_ready")]
    [TestCase("class", "@class")]
    public void CreateRecipeIdentifier_ShouldProduceValidIdentifiers(
        string recipeName,
        string expected)
    {
        FixtureFactorySourceEmitter.CreateRecipeIdentifier(recipeName).Should().Be(expected);
    }

    [Test]
    public void EmitSource_ShouldRejectRecipeIdentifierCollisions()
    {
        var configurations = new[]
        {
            CreateConfiguration(recipeName: "Approved user"),
            CreateConfiguration(recipeName: "Approved-user"),
        };

        Action action = () => FixtureFactorySourceEmitter.EmitSource(configurations);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*unique C# identifiers*");
    }

    private static FixtureGenerationSpec CreateConfiguration(
        string recipeName = "Validation")
    {
        return new FixtureGenerationSpec(
            "UserFixture",
            "Example.Generated",
            recipeName,
            "global::Example.User",
            "User",
            "global::Example.UserFixture.Baseline()",
            null,
            null,
            ImmutableArray<SubjectPropertySpec>.Empty,
            false,
            ImmutableArray<SubjectConstructorSpec>.Empty,
            false,
            false,
            null,
            ImmutableArray<DomainOperationContract>.Empty,
            null);
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var start = 0;
        while ((start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }
}
