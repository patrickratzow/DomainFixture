using System.Collections.Generic;
using DomainFixture.Contracts;
using DomainFixture.Pipeline;
using DomainFixture.TestGenerator.Boundaries;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;

namespace DomainFixture.SourceGenerator.Generation;

internal static class DomainBoundaryCaseProviderPipeline
{
    private static readonly ProviderPipeline<
        DomainConstraintContract,
        IReadOnlyList<GeneratedValidationCase>> Pipeline = new(
        new IPipelineProvider<DomainConstraintContract, IReadOnlyList<GeneratedValidationCase>>[]
        {
            new TextLengthBoundaryCaseProvider(),
            new TextPresenceBoundaryCaseProvider(),
            new Int32RangeBoundaryCaseProvider()
        },
        ProviderPipelineMode.ExactlyOne);

    public static ProviderResolution<IReadOnlyList<GeneratedValidationCase>> Resolve(
        DomainConstraintContract constraint) => Pipeline.Resolve(constraint);
}

internal sealed class TextLengthBoundaryCaseProvider :
    IPipelineProvider<DomainConstraintContract, IReadOnlyList<GeneratedValidationCase>>
{
    public string Id => "domainfixture.boundaries.text-length";

    public ProviderDecision<IReadOnlyList<GeneratedValidationCase>> Evaluate(
        DomainConstraintContract constraint)
    {
        if (constraint.KindId != DomainConstraintKinds.TextLength &&
            constraint.KindId != DomainConstraintKinds.TextMinimumLength &&
            constraint.KindId != DomainConstraintKinds.TextMaximumLength)
        {
            return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.NotHandled();
        }

        var property = new PropertyDescriptor(constraint.MemberPath);
        if (constraint.KindId == DomainConstraintKinds.TextLength)
        {
            if (!constraint.TryGetInt32(DomainConstraintParameters.Minimum, out var minimum) ||
                !constraint.TryGetInt32(DomainConstraintParameters.Maximum, out var maximum))
            {
                return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.Invalid(
                    "text length requires Int32 minimum and maximum parameters");
            }

            return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.Handled(
                new StringLengthBoundaryCaseGenerator().Generate(
                    new StringLengthConstraintDescriptor(
                        property,
                        minimum,
                        maximum,
                        constraint.FailureCode)));
        }

        if (constraint.KindId == DomainConstraintKinds.TextMinimumLength)
        {
            if (!constraint.TryGetInt32(DomainConstraintParameters.Minimum, out var minimum))
            {
                return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.Invalid(
                    "minimum text length requires an Int32 minimum parameter");
            }

            return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.Handled(
                new StringMinimumLengthBoundaryCaseGenerator().Generate(
                    new StringMinimumLengthConstraintDescriptor(
                        property,
                        minimum,
                        constraint.FailureCode)));
        }

        if (!constraint.TryGetInt32(DomainConstraintParameters.Maximum, out var parsedMaximum))
        {
            return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.Invalid(
                "maximum text length requires an Int32 maximum parameter");
        }

        return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.Handled(
            new StringMaximumLengthBoundaryCaseGenerator().Generate(
                new StringMaximumLengthConstraintDescriptor(
                    property,
                    parsedMaximum,
                    constraint.FailureCode)));
    }
}

internal sealed class TextPresenceBoundaryCaseProvider :
    IPipelineProvider<DomainConstraintContract, IReadOnlyList<GeneratedValidationCase>>
{
    public string Id => "domainfixture.boundaries.text-presence";

    public ProviderDecision<IReadOnlyList<GeneratedValidationCase>> Evaluate(
        DomainConstraintContract constraint)
    {
        if (constraint.KindId != DomainConstraintKinds.TextNotEmpty &&
            constraint.KindId != DomainConstraintKinds.TextNotNull)
        {
            return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.NotHandled();
        }

        var kind = constraint.KindId == DomainConstraintKinds.TextNotEmpty
            ? StringPresenceConstraintKind.NotEmpty
            : StringPresenceConstraintKind.NotNull;
        IReadOnlyList<GeneratedValidationCase> cases = new[]
        {
            new StringPresenceBoundaryCaseGenerator().Generate(
                new StringPresenceConstraintDescriptor(
                    new PropertyDescriptor(constraint.MemberPath),
                    kind,
                    constraint.FailureCode))
        };
        return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.Handled(cases);
    }
}

internal sealed class Int32RangeBoundaryCaseProvider :
    IPipelineProvider<DomainConstraintContract, IReadOnlyList<GeneratedValidationCase>>
{
    public string Id => "domainfixture.boundaries.int32-range";

    public ProviderDecision<IReadOnlyList<GeneratedValidationCase>> Evaluate(
        DomainConstraintContract constraint)
    {
        var hasMinimum = constraint.TryGetInt32(
            DomainConstraintParameters.Minimum,
            out var parsedMinimum);
        var hasMaximum = constraint.TryGetInt32(
            DomainConstraintParameters.Maximum,
            out var parsedMaximum);
        int? minimum;
        int? maximum;
        bool minimumInclusive;
        bool maximumInclusive;

        switch (constraint.KindId)
        {
            case DomainConstraintKinds.Int32InclusiveRange when hasMinimum && hasMaximum:
                minimum = parsedMinimum;
                maximum = parsedMaximum;
                minimumInclusive = true;
                maximumInclusive = true;
                break;
            case DomainConstraintKinds.Int32ExclusiveRange when hasMinimum && hasMaximum:
                minimum = parsedMinimum;
                maximum = parsedMaximum;
                minimumInclusive = false;
                maximumInclusive = false;
                break;
            case DomainConstraintKinds.Int32GreaterThan when hasMinimum:
                minimum = parsedMinimum;
                maximum = null;
                minimumInclusive = false;
                maximumInclusive = false;
                break;
            case DomainConstraintKinds.Int32LessThan when hasMaximum:
                minimum = null;
                maximum = parsedMaximum;
                minimumInclusive = false;
                maximumInclusive = false;
                break;
            case DomainConstraintKinds.Int32InclusiveRange:
            case DomainConstraintKinds.Int32ExclusiveRange:
            case DomainConstraintKinds.Int32GreaterThan:
            case DomainConstraintKinds.Int32LessThan:
                return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.Invalid(
                    "the Int32 constraint is missing a required boundary parameter");
            default:
                return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.NotHandled();
        }

        return ProviderDecision<IReadOnlyList<GeneratedValidationCase>>.Handled(
            new Int32RangeBoundaryCaseGenerator().Generate(
                new Int32RangeConstraintDescriptor(
                    new PropertyDescriptor(constraint.MemberPath),
                    minimum,
                    minimumInclusive,
                    maximum,
                    maximumInclusive,
                    constraint.FailureCode)));
    }
}
