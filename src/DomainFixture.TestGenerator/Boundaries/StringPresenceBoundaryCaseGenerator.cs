using System;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainFixture.TestGenerator.Boundaries;

public sealed class StringPresenceBoundaryCaseGenerator
{
    public GeneratedValidationCase Generate(StringPresenceConstraintDescriptor constraint)
    {
        if (constraint is null) throw new ArgumentNullException(nameof(constraint));

        var (name, value) = constraint.Kind switch
        {
            StringPresenceConstraintKind.NotEmpty => (
                "NotEmpty_Empty_IsInvalid",
                (ExpressionSyntax)LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal(string.Empty))),
            StringPresenceConstraintKind.NotNull => (
                "NotNull_Null_IsInvalid",
                (ExpressionSyntax)LiteralExpression(SyntaxKind.NullLiteralExpression)),
            _ => throw new ArgumentOutOfRangeException(nameof(constraint))
        };
        var mutation = new PropertyMutationDescriptor(constraint.Property, value);

        return new GeneratedValidationCase(
            $"{constraint.Property.Name}_{name}",
            mutation,
            ExpectedValidationOutcome.Invalid,
            constraint.ErrorCode);
    }
}
