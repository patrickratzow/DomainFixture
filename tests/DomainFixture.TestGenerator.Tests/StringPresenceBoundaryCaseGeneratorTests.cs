using DomainFixture.TestGenerator.Boundaries;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public class StringPresenceBoundaryCaseGeneratorTests
{
    [Test]
    public void Generate_ShouldCreateEmptyInvalidCase_ForNotEmpty()
    {
        var constraint = new StringPresenceConstraintDescriptor(
            new PropertyDescriptor("Description"),
            StringPresenceConstraintKind.NotEmpty,
            "DESCRIPTION_NOT_EMPTY");

        var result = new StringPresenceBoundaryCaseGenerator().Generate(constraint);

        result.Name.Should().Be("Description_NotEmpty_Empty_IsInvalid");
        result.ExpectedOutcome.Should().Be(ExpectedValidationOutcome.Invalid);
        result.ErrorCode.Should().Be("DESCRIPTION_NOT_EMPTY");
        result.Mutation.Value.NormalizeWhitespace().ToFullString().Should().Be("\"\"");
    }

    [Test]
    public void Generate_ShouldCreateNullInvalidCase_ForNotNull()
    {
        var constraint = new StringPresenceConstraintDescriptor(
            new PropertyDescriptor("Description"),
            StringPresenceConstraintKind.NotNull,
            "DESCRIPTION_REQUIRED");

        var result = new StringPresenceBoundaryCaseGenerator().Generate(constraint);

        result.Name.Should().Be("Description_NotNull_Null_IsInvalid");
        result.ExpectedOutcome.Should().Be(ExpectedValidationOutcome.Invalid);
        result.ErrorCode.Should().Be("DESCRIPTION_REQUIRED");
        result.Mutation.Value.NormalizeWhitespace().ToFullString().Should().Be("null");
    }
}
