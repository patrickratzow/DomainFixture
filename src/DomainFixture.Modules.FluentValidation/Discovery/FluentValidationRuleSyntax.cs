using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.Modules.FluentValidation;

internal static class FluentValidationRuleSyntax
{
    public static bool TryGetRuleProperty(
        SemanticModel semanticModel,
        InvocationExpressionSyntax validationInvocation,
        out IPropertySymbol property)
    {
        property = null!;
        var ruleForInvocation = FindRuleForInvocation(validationInvocation);
        if (ruleForInvocation is null || ruleForInvocation.ArgumentList.Arguments.Count == 0)
            return false;

        ExpressionSyntax? body = ruleForInvocation.ArgumentList.Arguments[0].Expression switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Body as ExpressionSyntax,
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.Body as ExpressionSyntax,
            _ => null
        };
        if (body is null || semanticModel.GetSymbolInfo(body).Symbol is not IPropertySymbol symbol)
            return false;
        property = symbol;
        return true;
    }

    public static string? FindErrorCode(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation)
    {
        if (invocation.Parent is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression == invocation &&
            memberAccess.Name.Identifier.ValueText == "WithErrorCode" &&
            memberAccess.Parent is InvocationExpressionSyntax outer &&
            outer.ArgumentList.Arguments.Count > 0)
        {
            var value = semanticModel.GetConstantValue(
                outer.ArgumentList.Arguments[0].Expression);
            return value.HasValue ? value.Value as string : null;
        }
        return null;
    }

    public static string InvocationName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => string.Empty
        };

    private static InvocationExpressionSyntax? FindRuleForInvocation(
        InvocationExpressionSyntax invocation)
    {
        var current = invocation;
        while (current.Expression is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Expression is InvocationExpressionSyntax previous)
        {
            if (InvocationName(previous) == "RuleFor")
                return previous;
            current = previous;
        }
        return null;
    }
}
