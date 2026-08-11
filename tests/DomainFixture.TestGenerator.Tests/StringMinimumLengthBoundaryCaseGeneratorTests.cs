using System.Linq;
using DomainFixture.TestGenerator.Boundaries;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public sealed class StringMinimumLengthBoundaryCaseGeneratorTests
{
    [Test]
    public void Generate_ShouldCreateBelowAndAtMinimumBoundaries()
    {
        var constraint = new StringMinimumLengthConstraintDescriptor(
            new PropertyDescriptor("Name"),
            minimum: 3,
            errorCode: "NAME_MINIMUM");

        var cases = new StringMinimumLengthBoundaryCaseGenerator().Generate(constraint);

        cases.Select(testCase => testCase.Name).Should().Equal(
            "Name_LengthBelowMinimum_IsInvalid",
            "Name_LengthAtMinimum_IsValid");
        cases.Select(testCase => testCase.Mutation.Value.NormalizeWhitespace().ToFullString())
            .Should().Equal("new string ('a', 2)", "new string ('a', 3)");
        cases[0].ExpectedOutcome.Should().Be(ExpectedValidationOutcome.Invalid);
        cases[0].ErrorCode.Should().Be("NAME_MINIMUM");
        cases[1].ExpectedOutcome.Should().Be(ExpectedValidationOutcome.Valid);
    }
}
