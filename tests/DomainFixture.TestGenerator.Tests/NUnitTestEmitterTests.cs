using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DomainFixture.TestGenerator.Framework.Emitters;
using DomainFixture.TestGenerator.Model;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public class NUnitTestEmitterTests
{
    private static readonly string[] TestNames =
    {
        "Registration_Baseline_IsValid",
        "Registration_Description_LengthBelowMinimum_IsInvalid",
        "Registration_Description_LengthAtMinimum_IsValid",
        "Registration_Description_LengthAtMaximum_IsValid",
        "Registration_Description_LengthAboveMaximum_IsInvalid"
    };

    [Test]
    public void EmitSource_ShouldBeDeterministicAndMatchSnapshot()
    {
        var emitter = new NUnitTestEmitter();
        var suite = CreateSuite();
        var version = typeof(NUnitTestEmitter).Assembly.GetName().Version!.ToString();

        var first = emitter.EmitSource(suite);
        var second = emitter.EmitSource(suite);

        first.Should().Be(second);
        first.Should().Be(
$@"using System.CodeDom.Compiler;
using NUnit.Framework;

namespace Generated.Tests
{{
    [GeneratedCode(""DomainFixture.TestGenerator"", ""{version}"")]
    [TestFixture]
    public sealed class UserRegistrationTests
    {{
        [Test]
        public void Registration_Baseline_IsValid()
        {{
            Assert.That(true, Is.True);
        }}

        [Test]
        public void Registration_Description_LengthBelowMinimum_IsInvalid()
        {{
            Assert.That(true, Is.True);
        }}

        [Test]
        public void Registration_Description_LengthAtMinimum_IsValid()
        {{
            Assert.That(true, Is.True);
        }}

        [Test]
        public void Registration_Description_LengthAtMaximum_IsValid()
        {{
            Assert.That(true, Is.True);
        }}

        [Test]
        public void Registration_Description_LengthAboveMaximum_IsInvalid()
        {{
            Assert.That(true, Is.True);
        }}
    }}
}}
");
    }

    [Test]
    public void EmitSource_ShouldParseWithoutErrors()
    {
        var source = new NUnitTestEmitter().EmitSource(CreateSuite());

        var errors = CSharpSyntaxTree.ParseText(source)
            .GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        errors.Should().BeEmpty();
    }

    [Test]
    public void EmitSource_ShouldCompileExposeFiveNUnitTestsAndExecute()
    {
        var source = new NUnitTestEmitter().EmitSource(CreateSuite());
        var assembly = Compile(source);
        var generatedType = assembly.GetType("Generated.Tests.UserRegistrationTests");

        generatedType.Should().NotBeNull();
        generatedType!.GetCustomAttribute<TestFixtureAttribute>().Should().NotBeNull();

        var methods = generatedType!.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.GetCustomAttribute<TestAttribute>() is not null)
            .OrderBy(method => method.Name)
            .ToArray();

        methods.Select(method => method.Name).Should().BeEquivalentTo(TestNames);

        var instance = Activator.CreateInstance(generatedType);
        foreach (var method in methods)
            method.Invoking(candidate => candidate.Invoke(instance, null)).Should().NotThrow();
    }

    [Test]
    public void Constructor_ShouldRejectDuplicateTestNames()
    {
        var duplicateTests = new[]
        {
            CreatePassingTest("Duplicate"),
            CreatePassingTest("Duplicate")
        };

        var create = () => new GeneratedTestSuite("Generated.Tests", "DuplicateTests", duplicateTests);

        create.Should().Throw<ArgumentException>()
            .WithMessage("*Duplicate*");
    }

    private static GeneratedTestSuite CreateSuite()
    {
        return new GeneratedTestSuite(
            "Generated.Tests",
            "UserRegistrationTests",
            TestNames.Select(CreatePassingTest));
    }

    private static GeneratedTest CreatePassingTest(string name)
    {
        return new GeneratedTest(name, new[] { CreatePassingAssertion() });
    }

    private static StatementSyntax CreatePassingAssertion()
    {
        return ExpressionStatement(
            InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Assert"),
                        IdentifierName("That")))
                .AddArgumentListArguments(
                    Argument(LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                    Argument(MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Is"),
                        IdentifierName("True")))));
    }

    private static Assembly Compile(string source)
    {
        var trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var references = new List<MetadataReference>(trustedPlatformAssemblies)
        {
            MetadataReference.CreateFromFile(typeof(TestAttribute).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            $"GeneratedTests_{Guid.NewGuid():N}",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);

        result.Success.Should().BeTrue(
            string.Join(Environment.NewLine, result.Diagnostics));

        return Assembly.Load(stream.ToArray());
    }
}
