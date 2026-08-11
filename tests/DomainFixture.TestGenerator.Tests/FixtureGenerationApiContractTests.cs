using System;
using System.Linq.Expressions;
using DomainFixture.Generation;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public sealed class FixtureGenerationApiContractTests
{
    [Test]
    public void ProfileOptions_ShouldExposeOperationsAndEntityIdentityConvention()
    {
        typeof(IFixtureGenerationOptions).GetMethod(nameof(IFixtureGenerationOptions.Operations))
            .Should().NotBeNull();
        typeof(IFixtureGenerationOptions).GetMethod(nameof(IFixtureGenerationOptions.Values))
            .Should().NotBeNull();
        typeof(IFixtureValueOptions).GetMethods().Should().ContainSingle(method =>
            method.Name == nameof(IFixtureValueOptions.For) &&
            method.GetGenericArguments().Length == 1);
        typeof(IFixtureConventionOptions).GetMethod(nameof(IFixtureConventionOptions.UseEntityIdentity))
            .Should().NotBeNull();
        typeof(IFixtureConventionOptions).GetMethod(nameof(IFixtureConventionOptions.AutoSynthesizeRecipes))
            .Should().NotBeNull();

        var rejectionMethods = typeof(IFixtureOperationOptions)
            .GetMethods();

        rejectionMethods.Should().Contain(method =>
            method.Name == nameof(IFixtureOperationOptions.RejectWith) &&
            method.GetGenericArguments().Length == 1);
        rejectionMethods.Should().Contain(method =>
            method.Name == nameof(IFixtureOperationOptions.RejectWith) &&
            method.GetGenericArguments().Length == 2);
        rejectionMethods.Should().Contain(method =>
            method.Name == nameof(IFixtureOperationOptions.UseResult) &&
            method.GetGenericArguments().Length == 2);
    }

    [Test]
    public void RecipeBuilder_ShouldExposeSynchronousTransitionDeclarations()
    {
        var builderType = typeof(IFixtureRecipeBuilder<ExampleSubject>);

        builderType.GetMethods().Should().Contain(method =>
            method.Name == nameof(IFixtureRecipeBuilder<ExampleSubject>.Transition));
        builderType.GetMethod(nameof(IFixtureRecipeBuilder<ExampleSubject>.RejectTransition))
            .Should().NotBeNull();
        builderType.GetMethods().Should().Contain(method =>
            method.Name == nameof(IFixtureRecipeBuilder<ExampleSubject>.State));
        builderType.GetMethod(nameof(IFixtureRecipeBuilder<ExampleSubject>.FromTransition))
            .Should().NotBeNull();
        builderType.GetMethods().Should().Contain(method =>
            method.Name == nameof(IFixtureRecipeBuilder<ExampleSubject>.Transition) &&
            method.GetParameters().Length == 3);
    }

    // These calls are intentionally never executed. They keep the intended fluent syntax
    // under compilation in the assembly that owns the public API contract tests.
    private static void CompileProfileSyntax(IFixtureGenerationOptions options)
    {
        options.Operations()
            .RejectWith<InvalidOperationException>()
            .RejectWith<ExampleSubject, ArgumentException>()
            .UseResult<ExampleSubject, ExampleResult>(
                result => result.Succeeded,
                result => result.Value);
        options.Conventions()
            .AutoSynthesizeRecipes()
            .UseEntityIdentity();
        options.Values()
            .For<ExampleStatus>(() => ExampleStatus.Pending)
            .For<string>(() => "configured");
    }

    private static void CompileRecipeSyntax(IFixtureRecipeBuilder<ExampleSubject> recipe)
    {
        recipe.FromTransition("Pending", "Approve")
            .Transition(
                "Approve",
                subject => subject.Approve(),
                subject => subject.Status,
                ExampleStatus.Approved)
            .Transition(
                "Approve immutable",
                subject => subject.WithStatus(ExampleStatus.Approved),
                subject => subject.Status,
                ExampleStatus.Approved)
            .Transition(
                "Try approve",
                subject => subject.TryApprove(FixtureValue.Auto<string>()),
                result => result.Succeeded)
            .State(
                "Pending",
                subject => subject.Status,
                ExampleStatus.Pending)
            .RejectTransition<InvalidOperationException>(
                "ApproveAgain",
                subject => subject.Assign(FixtureValue.Auto<string>()));
    }

    private sealed class ExampleSubject
    {
        public ExampleStatus Status { get; private set; }

        public void Approve() => Status = ExampleStatus.Approved;

        public void Assign(string owner)
        {
        }

        public ExampleSubject WithStatus(ExampleStatus status) => new() { Status = status };

        public ExampleResult TryApprove(string owner) => new() { Succeeded = true };
    }

    private sealed class ExampleResult
    {
        public bool Succeeded { get; init; }
        public ExampleSubject Value { get; init; } = new();
    }

    private enum ExampleStatus
    {
        Pending,
        Approved
    }

    [Test]
    public void AutoFixtureValue_ShouldFailIfExecutedOutsideGeneration()
    {
        var action = () => FixtureValue.Auto<string>();

        action.Should().Throw<NotSupportedException>()
            .WithMessage("*source-generation marker*");
    }
}
