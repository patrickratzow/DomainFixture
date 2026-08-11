using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.Modules.FluentValidation;

internal static class FluentValidationConstraintMapper
{
    public static bool TryMap(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IPropertySymbol property,
        string methodName,
        out MappedConstraint constraint,
        out string failureReason)
    {
        constraint = default;
        failureReason = string.Empty;

        switch (methodName)
        {
            case "Length":
                if (!IsString(property) ||
                    !TryReadTwoIntArguments(semanticModel, invocation, out var lengthMinimum, out var lengthMaximum) ||
                    lengthMinimum < 0 || lengthMaximum < lengthMinimum)
                    return Fail("constant non-negative string bounds are required", out failureReason);
                constraint = new MappedConstraint(
                    "domainfixture.text.length",
                    lengthMinimum,
                    lengthMaximum);
                return true;
            case "MinimumLength":
                if (!IsString(property) ||
                    !TryReadSingleIntArgument(semanticModel, invocation, out var parsedMinimum) ||
                    parsedMinimum < 0)
                    return Fail("a constant non-negative string bound is required", out failureReason);
                constraint = new MappedConstraint(
                    "domainfixture.text.minimum-length",
                    parsedMinimum,
                    null);
                return true;
            case "MaximumLength":
                if (!IsString(property) ||
                    !TryReadSingleIntArgument(semanticModel, invocation, out var parsedMaximum) ||
                    parsedMaximum < 0)
                    return Fail("a constant non-negative string bound is required", out failureReason);
                constraint = new MappedConstraint(
                    "domainfixture.text.maximum-length",
                    null,
                    parsedMaximum);
                return true;
            case "NotEmpty":
                if (!IsString(property))
                    return Fail("only text presence constraints are supported", out failureReason);
                constraint = new MappedConstraint("domainfixture.text.not-empty", null, null);
                return true;
            case "NotNull":
                if (!IsString(property))
                    return Fail("only text presence constraints are supported", out failureReason);
                constraint = new MappedConstraint("domainfixture.text.not-null", null, null);
                return true;
            case "InclusiveBetween":
                if (!IsInt32(property) ||
                    !TryReadTwoIntArguments(semanticModel, invocation, out var inclusiveMinimum, out var inclusiveMaximum) ||
                    inclusiveMaximum < inclusiveMinimum)
                    return Fail("constant Int32 bounds in ascending order are required", out failureReason);
                constraint = new MappedConstraint(
                    "domainfixture.int32.inclusive-range",
                    inclusiveMinimum,
                    inclusiveMaximum);
                return true;
            case "ExclusiveBetween":
                if (!IsInt32(property) ||
                    !TryReadTwoIntArguments(semanticModel, invocation, out var exclusiveMinimum, out var exclusiveMaximum) ||
                    (long)exclusiveMaximum - exclusiveMinimum <= 1)
                    return Fail("constant Int32 bounds containing a valid value are required", out failureReason);
                constraint = new MappedConstraint(
                    "domainfixture.int32.exclusive-range",
                    exclusiveMinimum,
                    exclusiveMaximum);
                return true;
            case "GreaterThan":
                if (!IsInt32(property) ||
                    !TryReadSingleIntArgument(semanticModel, invocation, out var greaterThan) ||
                    greaterThan == int.MaxValue)
                    return Fail("a constant Int32 bound with a representable valid value is required", out failureReason);
                constraint = new MappedConstraint(
                    "domainfixture.int32.greater-than",
                    greaterThan,
                    null);
                return true;
            case "LessThan":
                if (!IsInt32(property) ||
                    !TryReadSingleIntArgument(semanticModel, invocation, out var lessThan) ||
                    lessThan == int.MinValue)
                    return Fail("a constant Int32 bound with a representable valid value is required", out failureReason);
                constraint = new MappedConstraint(
                    "domainfixture.int32.less-than",
                    null,
                    lessThan);
                return true;
            default:
                return Fail("the rule is not supported", out failureReason);
        }
    }

    private static bool Fail(string reason, out string failureReason)
    {
        failureReason = reason;
        return false;
    }

    private static bool TryReadSingleIntArgument(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        out int value) =>
        TryReadIntArgument(semanticModel, invocation, 0, out value);

    private static bool TryReadTwoIntArguments(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        out int minimum,
        out int maximum)
    {
        minimum = 0;
        maximum = 0;
        return TryReadIntArgument(semanticModel, invocation, 0, out minimum) &&
               TryReadIntArgument(semanticModel, invocation, 1, out maximum);
    }

    private static bool TryReadIntArgument(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        int index,
        out int value)
    {
        value = 0;
        if (invocation.ArgumentList.Arguments.Count <= index)
            return false;
        var constant = semanticModel.GetConstantValue(
            invocation.ArgumentList.Arguments[index].Expression);
        if (!constant.HasValue || constant.Value is not int parsed)
            return false;
        value = parsed;
        return true;
    }

    private static bool IsString(IPropertySymbol property) =>
        property.Type.SpecialType == SpecialType.System_String;

    private static bool IsInt32(IPropertySymbol property) =>
        property.Type.SpecialType == SpecialType.System_Int32;
}

internal readonly struct MappedConstraint
{
    public string KindId { get; }
    public int? Minimum { get; }
    public int? Maximum { get; }

    public MappedConstraint(string kindId, int? minimum, int? maximum)
    {
        KindId = kindId;
        Minimum = minimum;
        Maximum = maximum;
    }
}
