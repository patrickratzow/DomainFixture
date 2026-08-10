using System;
using System.Collections.Generic;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainFixture.TestGenerator.Boundaries;

public sealed class StringLengthBoundaryCaseGenerator
{
    public IReadOnlyList<GeneratedValidationCase> Generate(StringLengthConstraintDescriptor constraint)
    {
        if (constraint is null) throw new ArgumentNullException(nameof(constraint));

        var cases = new List<GeneratedValidationCase>();

        if (constraint.Minimum > 0)
        {
            cases.Add(CreateCase(
                constraint,
                "LengthBelowMinimum_IsInvalid",
                constraint.Minimum - 1,
                ExpectedValidationOutcome.Invalid));
        }

        cases.Add(CreateCase(
            constraint,
            "LengthAtMinimum_IsValid",
            constraint.Minimum,
            ExpectedValidationOutcome.Valid));

        if (constraint.Maximum != constraint.Minimum)
        {
            cases.Add(CreateCase(
                constraint,
                "LengthAtMaximum_IsValid",
                constraint.Maximum,
                ExpectedValidationOutcome.Valid));
        }

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
        StringLengthConstraintDescriptor constraint,
        string suffix,
        int length,
        ExpectedValidationOutcome expectedOutcome)
    {
        var mutation = new PropertyMutationDescriptor(
            constraint.Property,
            CreateStringExpression(length));

        return new GeneratedValidationCase(
            $"{constraint.Property.Name}_{suffix}",
            mutation,
            expectedOutcome,
            expectedOutcome == ExpectedValidationOutcome.Invalid ? constraint.ErrorCode : null);
    }

    private static ExpressionSyntax CreateStringExpression(int length)
    {
        return ObjectCreationExpression(PredefinedType(Token(SyntaxKind.StringKeyword)))
            .AddArgumentListArguments(
                Argument(LiteralExpression(
                    SyntaxKind.CharacterLiteralExpression,
                    Literal('a'))),
                Argument(LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(length))));
    }
}
