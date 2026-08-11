using System.Linq;
using DomainFixture.TestGenerator.Boundaries;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public sealed class Int32RangeBoundaryCaseGeneratorTests
{
    [Test]
    public void Generate_ShouldCreateBelowAtAtAndAboveInclusiveBoundaries()
    {
        var constraint = new Int32RangeConstraintDescriptor(
            new PropertyDescriptor("Level"),
            minimum: 1,
            minimumInclusive: true,
            maximum: 10,
            maximumInclusive: true,
            errorCode: "LEVEL_RANGE");

        var cases = new Int32RangeBoundaryCaseGenerator().Generate(constraint);

        cases.Select(testCase => testCase.Name).Should().Equal(
            "Level_BelowMinimum_IsInvalid",
            "Level_AtMinimum_IsValid",
            "Level_AtMaximum_IsValid",
            "Level_AboveMaximum_IsInvalid");
        cases.Select(testCase => testCase.Mutation.Value.NormalizeWhitespace().ToFullString())
            .Should().Equal("0", "1", "10", "11");
        cases.Where(testCase => testCase.ExpectedOutcome == ExpectedValidationOutcome.Invalid)
            .Should().OnlyContain(testCase => testCase.ErrorCode == "LEVEL_RANGE");
    }

    [Test]
    public void Generate_ShouldTreatExclusiveBoundsAsInvalid()
    {
        var constraint = new Int32RangeConstraintDescriptor(
            new PropertyDescriptor("Level"),
            minimum: 1,
            minimumInclusive: false,
            maximum: 10,
            maximumInclusive: false);

        var cases = new Int32RangeBoundaryCaseGenerator().Generate(constraint);

        cases.Select(testCase => testCase.Name).Should().Equal(
            "Level_AtExclusiveMinimum_IsInvalid",
            "Level_AboveExclusiveMinimum_IsValid",
            "Level_BelowExclusiveMaximum_IsValid",
            "Level_AtExclusiveMaximum_IsInvalid");
        cases.Select(testCase => testCase.Mutation.Value.NormalizeWhitespace().ToFullString())
            .Should().Equal("1", "2", "9", "10");
    }
}
