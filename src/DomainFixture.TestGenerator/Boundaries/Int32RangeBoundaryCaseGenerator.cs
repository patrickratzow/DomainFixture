using System;
using System.Collections.Generic;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainFixture.TestGenerator.Boundaries;

public sealed class Int32RangeBoundaryCaseGenerator
{
    public IReadOnlyList<GeneratedValidationCase> Generate(Int32RangeConstraintDescriptor constraint)
    {
        if (constraint is null) throw new ArgumentNullException(nameof(constraint));

        var cases = new List<GeneratedValidationCase>();
        if (constraint.Minimum is int minimum)
        {
            if (constraint.MinimumInclusive)
            {
                if (minimum > int.MinValue)
                    cases.Add(CreateCase(constraint, "BelowMinimum_IsInvalid", minimum - 1, false));
                cases.Add(CreateCase(constraint, "AtMinimum_IsValid", minimum, true));
            }
            else
            {
                cases.Add(CreateCase(constraint, "AtExclusiveMinimum_IsInvalid", minimum, false));
                if (minimum < int.MaxValue)
                    cases.Add(CreateCase(constraint, "AboveExclusiveMinimum_IsValid", minimum + 1, true));
            }
        }

        if (constraint.Maximum is int maximum)
        {
            if (constraint.MaximumInclusive)
            {
                cases.Add(CreateCase(constraint, "AtMaximum_IsValid", maximum, true));
                if (maximum < int.MaxValue)
                    cases.Add(CreateCase(constraint, "AboveMaximum_IsInvalid", maximum + 1, false));
            }
            else
            {
                if (maximum > int.MinValue)
                    cases.Add(CreateCase(constraint, "BelowExclusiveMaximum_IsValid", maximum - 1, true));
                cases.Add(CreateCase(constraint, "AtExclusiveMaximum_IsInvalid", maximum, false));
            }
        }

        return cases;
    }

    private static GeneratedValidationCase CreateCase(
        Int32RangeConstraintDescriptor constraint,
        string suffix,
        int value,
        bool valid)
    {
        var outcome = valid
            ? ExpectedValidationOutcome.Valid
            : ExpectedValidationOutcome.Invalid;
        return new GeneratedValidationCase(
            $"{constraint.Property.Name}_{suffix}",
            new PropertyMutationDescriptor(
                constraint.Property,
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value))),
            outcome,
            valid ? null : constraint.ErrorCode);
    }
}
