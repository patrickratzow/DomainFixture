using System.Linq;
using DomainFixture.TestGenerator.Boundaries;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public class StringLengthBoundaryCaseGeneratorTests
{
    [Test]
    public void Generate_ShouldCreateFourBoundaryCases()
    {
        var constraint = new StringLengthConstraintDescriptor(
            new PropertyDescriptor("Description"),
            minimum: 4,
            maximum: 8,
            errorCode: "DESCRIPTION_LENGTH");

        var cases = new StringLengthBoundaryCaseGenerator().Generate(constraint);

        cases.Select(@case => @case.Name).Should().Equal(
            "Description_LengthBelowMinimum_IsInvalid",
            "Description_LengthAtMinimum_IsValid",
            "Description_LengthAtMaximum_IsValid",
            "Description_LengthAboveMaximum_IsInvalid");
        cases.Select(@case => @case.ExpectedOutcome).Should().Equal(
            ExpectedValidationOutcome.Invalid,
            ExpectedValidationOutcome.Valid,
            ExpectedValidationOutcome.Valid,
            ExpectedValidationOutcome.Invalid);
        cases.Select(@case => @case.Mutation.Value.NormalizeWhitespace().ToFullString()).Should().Equal(
            "new string ('a', 3)",
            "new string ('a', 4)",
            "new string ('a', 8)",
            "new string ('a', 9)");
        cases.Where(@case => @case.ExpectedOutcome == ExpectedValidationOutcome.Invalid)
            .Should().OnlyContain(@case => @case.ErrorCode == "DESCRIPTION_LENGTH");
    }
}
