using DomainFixture.Contracts;
using DomainFixture.Generation.Metadata;
using DomainFixture.TestGenerator.Modules;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public sealed class CompileTimeModuleCatalogTests
{
    [Test]
    public void Catalog_ShouldAcceptRecipeFromDeclaredCapableModule()
    {
        var module = Module(DomainFixtureModuleCapabilities.Recipes);
        var recipe = Recipe();

        var catalog = CompileTimeModuleCatalog.Create(new[] { module }, new[] { recipe });

        catalog.Issues.Should().BeEmpty();
        catalog.Modules.Should().ContainSingle().Which.Should().BeSameAs(module);
        catalog.Recipes.Should().ContainSingle().Which.Should().BeSameAs(recipe);
    }

    [Test]
    public void Catalog_ShouldRejectRecipeWhenModuleDidNotDeclareCapability()
    {
        var catalog = CompileTimeModuleCatalog.Create(
            new[] { Module(DomainFixtureModuleCapabilities.Constraints) },
            new[] { Recipe() });

        catalog.Recipes.Should().BeEmpty();
        catalog.Issues.Should().ContainSingle(issue =>
            issue.Kind == CompileTimeModuleIssueKind.UnsupportedCapability);
    }

    [Test]
    public void Catalog_ShouldRejectConflictingModuleIdentity()
    {
        var catalog = CompileTimeModuleCatalog.Create(
            new[]
            {
                Module(DomainFixtureModuleCapabilities.Recipes),
                new CompileTimeModuleDescriptor(
                    DomainFixtureModuleManifestAttribute.CurrentSchemaVersion,
                    "example.requests",
                    "2.0.0",
                    "global::Example.RequestModule",
                    new[] { DomainFixtureModuleCapabilities.Recipes })
            },
            new[] { Recipe() });

        catalog.Modules.Should().BeEmpty();
        catalog.Recipes.Should().BeEmpty();
        catalog.Issues.Should().Contain(issue =>
            issue.Kind == CompileTimeModuleIssueKind.ConflictingModule);
        catalog.Issues.Should().Contain(issue =>
            issue.Kind == CompileTimeModuleIssueKind.UnknownModule);
    }

    [Test]
    public void Catalog_ShouldResolveDeclaredCapabilities_AndPreserveLegacySources()
    {
        var catalog = CompileTimeModuleCatalog.Create(
            new[] { Module(DomainFixtureModuleCapabilities.Recipes) },
            new CompileTimeRecipeContribution[0]);

        catalog.ResolveCapability(
                "global::Example.RequestModule",
                DomainFixtureModuleCapabilities.Recipes)
            .Status.Should().Be(CompileTimeModuleCapabilityStatus.Supported);
        catalog.ResolveCapability(
                "global::Example.RequestModule",
                DomainFixtureModuleCapabilities.Constraints)
            .Status.Should().Be(CompileTimeModuleCapabilityStatus.MissingCapability);
        catalog.ResolveCapability(
                "global::Legacy.Adapter",
                DomainFixtureModuleCapabilities.Constraints)
            .Status.Should().Be(CompileTimeModuleCapabilityStatus.Unregistered);
    }

    [Test]
    public void MetadataContracts_ShouldExposeStableCurrentSchemas()
    {
        DomainFixtureModuleManifestAttribute.CurrentSchemaVersion.Should().Be(1);
        DomainFixtureRecipeManifestAttribute.CurrentSchemaVersion.Should().Be(1);
        DomainFixtureModuleCapabilities.Recipes.Should().Be("domainfixture.module.recipes");
    }

    private static CompileTimeModuleDescriptor Module(string capability) =>
        new(
            DomainFixtureModuleManifestAttribute.CurrentSchemaVersion,
            "example.requests",
            "1.0.0",
            "global::Example.RequestModule",
            new[] { capability });

    private static CompileTimeRecipeContribution Recipe() =>
        new(
            DomainFixtureRecipeManifestAttribute.CurrentSchemaVersion,
            "example.requests",
            "global::Example.RequestModule",
            "global::Example.PlaceOrder",
            "Example.Generated",
            "PlaceOrderFixture",
            "Valid");
}
