using System.Linq;
using DomainFixture.TestGenerator.Boundaries;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public sealed class StringMaximumLengthBoundaryCaseGeneratorTests
{
    [Test]
    public void Generate_ShouldCreateMaximumAndAboveMaximumBoundaries()
    {
        var constraint = new StringMaximumLengthConstraintDescriptor(
            new PropertyDescriptor("Value"),
            maximum: 128,
            errorCode: "MAXIMUM_LENGTH");

        var cases = new StringMaximumLengthBoundaryCaseGenerator().Generate(constraint);

        cases.Select(testCase => testCase.Name).Should().Equal(
            "Value_LengthAtMaximum_IsValid",
            "Value_LengthAboveMaximum_IsInvalid");
        cases[0].ExpectedOutcome.Should().Be(ExpectedValidationOutcome.Valid);
        cases[1].ExpectedOutcome.Should().Be(ExpectedValidationOutcome.Invalid);
        cases[1].ErrorCode.Should().Be("MAXIMUM_LENGTH");
    }
}
