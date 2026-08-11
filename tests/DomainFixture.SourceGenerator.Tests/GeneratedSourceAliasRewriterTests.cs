using System.Linq;
using DomainFixture.SourceGenerator.Emission;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class GeneratedSourceAliasRewriterTests
{
    [Test]
    public void Rewrite_ShouldAliasWholeTypeReferences_WithoutRewritingLongerTypeNames()
    {
        const string source = @"
namespace Tests.Generation
{
    public sealed class Suite
    {
        private global::Domain.ValueObjects.QualifiedHandle _subject;
        private global::Domain.ValueObjects.QualifiedHandleValidator _validator;
    }
}";

        var result = GeneratedSourceAliasRewriter.Rewrite(
            source,
            new[]
            {
                "global::Domain.ValueObjects.QualifiedHandle",
                "global::Domain.ValueObjects.QualifiedHandleValidator"
            },
            "Tests.Generation");

        result.Should().Contain("using QualifiedHandle = global::Domain.ValueObjects.QualifiedHandle;")
            .And.Contain("using QualifiedHandleValidator = global::Domain.ValueObjects.QualifiedHandleValidator;")
            .And.Contain("QualifiedHandle _subject;")
            .And.Contain("QualifiedHandleValidator _validator;");
        AssertValidSyntax(result);
    }

    [Test]
    public void Rewrite_ShouldUseSymmetricNamespaceAliases_WhenShortNamesCollide()
    {
        const string source = @"
namespace Tests.Generation
{
    public sealed class Suite
    {
        private global::Sales.Domain.Order _sales;
        private global::Shipping.Domain.Order _shipping;
    }
}";

        var result = GeneratedSourceAliasRewriter.Rewrite(
            source,
            new[] { "global::Sales.Domain.Order", "global::Shipping.Domain.Order" },
            "Tests.Generation");

        result.Should().Contain("using SalesDomainOrder = global::Sales.Domain.Order;")
            .And.Contain("using ShippingDomainOrder = global::Shipping.Domain.Order;")
            .And.Contain("SalesDomainOrder _sales;")
            .And.Contain("ShippingDomainOrder _shipping;");
        AssertValidSyntax(result);
    }

    [Test]
    public void Rewrite_ShouldQualifyAlias_WhenSimpleNameIsShadowedInGeneratedScope()
    {
        const string source = @"
namespace Tests.Generation
{
    public sealed class Suite
    {
        public void Run()
        {
            var Order = 42;
            global::Sales.Domain.Order subject = null;
        }
    }
}";

        var result = GeneratedSourceAliasRewriter.Rewrite(
            source,
            new[] { "global::Sales.Domain.Order" },
            "Tests.Generation");

        result.Should().Contain("using DomainOrder = global::Sales.Domain.Order;")
            .And.Contain("DomainOrder subject = null;");
        AssertValidSyntax(result);
    }

    [Test]
    public void Rewrite_ShouldAliasClosedGenericTypes_AndSimplifySafeFrameworkNames()
    {
        const string source = @"
namespace Tests.Generation
{
    public sealed class Suite
    {
        public global::Domain.Result<global::Domain.Subject> Run()
        {
            global::NUnit.Framework.Assert.That(true, global::NUnit.Framework.Is.True);
            return null;
        }
    }
}";

        var result = GeneratedSourceAliasRewriter.Rewrite(
            source,
            new[]
            {
                "global::Domain.Result<global::Domain.Subject>",
                "global::Domain.Subject"
            },
            "Tests.Generation");

        result.Should().Contain("using Result = global::Domain.Result<global::Domain.Subject>;")
            .And.Contain("using NUnit.Framework;")
            .And.Contain("public Result Run()")
            .And.Contain("Assert.That(true, Is.True);");
        AssertValidSyntax(result);
    }

    [Test]
    public void Rewrite_ShouldKeepGlobalFrameworkName_WhenShortNameIsShadowed()
    {
        const string source = @"
namespace Tests.Generation
{
    public sealed class Suite
    {
        public void Run()
        {
            var ArgumentException = 42;
            global::System.ArgumentException failure = null;
        }
    }
}";

        var result = GeneratedSourceAliasRewriter.Rewrite(
            source,
            new[] { "global::System.ArgumentException" },
            "Tests.Generation");

        result.Should().Contain("global::System.ArgumentException failure = null;")
            .And.NotContain("using System;");
        AssertValidSyntax(result);
    }

    [Test]
    public void Rewrite_ShouldUseShortName_ForTypeDeclaredInGeneratedFile()
    {
        const string source = @"
namespace Tests.Generation
{
    internal sealed class Helper {}

    public sealed class Suite
    {
        private global::Tests.Generation.Helper _helper;
    }
}";

        var result = GeneratedSourceAliasRewriter.Rewrite(
            source,
            new[] { "global::Tests.Generation.Helper" },
            "Tests.Generation");

        result.Should().Contain("private Helper _helper;")
            .And.NotContain("using GenerationHelper");
        AssertValidSyntax(result);
    }

    private static void AssertValidSyntax(string source)
    {
        CSharpSyntaxTree.ParseText(source)
            .GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }
}
