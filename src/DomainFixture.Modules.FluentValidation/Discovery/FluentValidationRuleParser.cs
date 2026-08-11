using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.Modules.FluentValidation;

internal static class FluentValidationRuleParser
{
    public static RuleContribution? TryCreate(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol subjectType,
        string methodName,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (!FluentValidationRuleSyntax.TryGetRuleProperty(
                semanticModel,
                invocation,
                out var property))
        {
            diagnostics.Add(CannotInterpret(
                invocation,
                methodName,
                "RuleFor must target a direct property"));
            return null;
        }

        if (!FluentValidationConstraintMapper.TryMap(
                semanticModel,
                invocation,
                property,
                methodName,
                out var constraint,
                out var failureReason))
        {
            diagnostics.Add(CannotInterpret(invocation, methodName, failureReason));
            return null;
        }

        return new RuleContribution(
            GeneratedTypeNames.Display(subjectType),
            property.Name,
            constraint.KindId,
            constraint.Minimum,
            constraint.Maximum,
            FluentValidationRuleSyntax.FindErrorCode(semanticModel, invocation),
            HasAccessibleSetter(property));
    }

    public static string InvocationName(InvocationExpressionSyntax invocation) =>
        FluentValidationRuleSyntax.InvocationName(invocation);

    public static bool IsRuleCandidate(string methodName) => methodName is
        "Length" or "MinimumLength" or "MaximumLength" or "NotEmpty" or "NotNull" or
        "InclusiveBetween" or "ExclusiveBetween" or "GreaterThan" or "LessThan" or
        "EmailAddress" or "Matches" or "Must" or "Equal" or "NotEqual" or "Empty" or
        "Null" or "IsInEnum" or "CreditCard" or "PrecisionScale";

    public static bool IsSupported(string methodName) => methodName is
        "Length" or "MinimumLength" or "MaximumLength" or "NotEmpty" or "NotNull" or
        "InclusiveBetween" or "ExclusiveBetween" or "GreaterThan" or "LessThan";

    private static Diagnostic CannotInterpret(
        InvocationExpressionSyntax invocation,
        string methodName,
        string reason) =>
        Diagnostic.Create(
            FluentValidationDiagnostics.UninterpretableRule,
            invocation.GetLocation(),
            methodName,
            reason);

    private static bool HasAccessibleSetter(IPropertySymbol property) =>
        property.SetMethod?.IsInitOnly != true &&
        property.SetMethod?.DeclaredAccessibility is Accessibility.Public or
            Accessibility.Internal or Accessibility.ProtectedOrInternal;
}
