using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DomainFixture.TestGenerator.Boundaries;
using DomainFixture.TestGenerator.Framework.Emitters;
using DomainFixture.TestGenerator.Generation;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using DomainFixture.Validation;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public class ValidationTestGenerationTests
{
    [Test]
    public void GeneratedValidationTests_ShouldCompileDiscoverAndExecute()
    {
        var property = new PropertyDescriptor("Description");
        var constraint = new StringLengthConstraintDescriptor(
            property,
            minimum: 4,
            maximum: 8,
            errorCode: "DESCRIPTION_LENGTH");
        var cases = new StringLengthBoundaryCaseGenerator().Generate(constraint);
        var descriptor = new ValidationTestSuiteDescriptor(
            "Generated.ValidationTests",
            "UserRegistrationValidationTests",
            "Registration",
            IdentifierName("User"),
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("UserRecipes"),
                    IdentifierName("Registration"))),
            ObjectCreationExpression(IdentifierName("RegistrationUserValidator"))
                .WithArgumentList(ArgumentList()),
            cases);

        var suite = new ValidationTestSuiteBuilder().Build(descriptor);
        var generatedSource = new NUnitTestEmitter().EmitSource(suite);
        var assembly = Compile(generatedSource, SupportSource);
        var generatedType = assembly.GetType(
            "Generated.ValidationTests.UserRegistrationValidationTests");

        generatedType.Should().NotBeNull();
        var methods = generatedType!.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.GetCustomAttribute<TestAttribute>() is not null)
            .OrderBy(method => method.Name)
            .ToArray();

        methods.Should().HaveCount(5);
        methods.Select(method => method.Name).Should().BeEquivalentTo(
            "Registration_Baseline_IsValid",
            "Registration_Description_LengthBelowMinimum_IsInvalid",
            "Registration_Description_LengthAtMinimum_IsValid",
            "Registration_Description_LengthAtMaximum_IsValid",
            "Registration_Description_LengthAboveMaximum_IsInvalid");

        var instance = Activator.CreateInstance(generatedType);
        foreach (var method in methods)
        {
            var invocation = method.Invoke(instance, null);
            invocation.Should().BeNull();
        }
    }

    private static Assembly Compile(params string[] sources)
    {
        var trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var references = new List<MetadataReference>(trustedPlatformAssemblies)
        {
            MetadataReference.CreateFromFile(typeof(TestAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IFixtureValidator<>).Assembly.Location)
        };
        var syntaxTrees = sources.Select(source => CSharpSyntaxTree.ParseText(source));
        var compilation = CSharpCompilation.Create(
            $"GeneratedValidationTests_{Guid.NewGuid():N}",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);

        result.Success.Should().BeTrue(
            string.Join(Environment.NewLine, result.Diagnostics));

        return Assembly.Load(stream.ToArray());
    }

    private const string SupportSource = @"
using System.Collections.Generic;
using DomainFixture.Validation;

namespace Generated.ValidationTests
{
    public sealed class User
    {
        public string Description { get; set; } = string.Empty;
    }

    public static class UserRecipes
    {
        public static User Registration()
        {
            return new User { Description = ""baseline"" };
        }
    }

    public sealed class RegistrationUserValidator : IFixtureValidator<User>
    {
        public ValidationReport Validate(User subject)
        {
            var failures = new List<ValidationFailure>();

            if (subject.Description.Length < 4 || subject.Description.Length > 8)
            {
                failures.Add(new ValidationFailure(
                    ""Description"",
                    ""DESCRIPTION_LENGTH""));
            }

            return new ValidationReport(failures);
        }
    }
}";
}
