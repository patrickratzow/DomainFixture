using System;
using System.Collections.Generic;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainFixture.TestGenerator.Boundaries;

public sealed class StringMaximumLengthBoundaryCaseGenerator
{
    public IReadOnlyList<GeneratedValidationCase> Generate(
        StringMaximumLengthConstraintDescriptor constraint)
    {
        if (constraint is null) throw new ArgumentNullException(nameof(constraint));

        var cases = new List<GeneratedValidationCase>
        {
            CreateCase(
                constraint,
                "LengthAtMaximum_IsValid",
                constraint.Maximum,
                ExpectedValidationOutcome.Valid)
        };

        if (constraint.Maximum < int.MaxValue)
        {
            cases.Add(CreateCase(
                constraint,
                "LengthAboveMaximum_IsInvalid",
                constraint.Maximum + 1,
                ExpectedValidationOutcome.Invalid));
        }

        return cases;
    }

    private static GeneratedValidationCase CreateCase(
        StringMaximumLengthConstraintDescriptor constraint,
        string suffix,
        int length,
        ExpectedValidationOutcome expectedOutcome)
    {
        var value = ObjectCreationExpression(
                PredefinedType(Token(SyntaxKind.StringKeyword)))
            .AddArgumentListArguments(
                Argument(LiteralExpression(
                    SyntaxKind.CharacterLiteralExpression,
                    Literal('a'))),
                Argument(LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(length))));

        return new GeneratedValidationCase(
            $"{constraint.Property.Name}_{suffix}",
            new PropertyMutationDescriptor(constraint.Property, value),
            expectedOutcome,
            expectedOutcome == ExpectedValidationOutcome.Invalid
                ? constraint.ErrorCode
                : null);
    }
}
