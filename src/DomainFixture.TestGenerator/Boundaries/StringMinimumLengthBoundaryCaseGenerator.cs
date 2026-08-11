using System;
using System.Collections.Generic;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainFixture.TestGenerator.Boundaries;

public sealed class StringMinimumLengthBoundaryCaseGenerator
{
    public IReadOnlyList<GeneratedValidationCase> Generate(
        StringMinimumLengthConstraintDescriptor constraint)
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
        return cases;
    }

    private static GeneratedValidationCase CreateCase(
        StringMinimumLengthConstraintDescriptor constraint,
        string suffix,
        int length,
        ExpectedValidationOutcome outcome)
    {
        var value = ObjectCreationExpression(PredefinedType(Token(SyntaxKind.StringKeyword)))
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
            outcome,
            outcome == ExpectedValidationOutcome.Invalid ? constraint.ErrorCode : null);
    }
}
