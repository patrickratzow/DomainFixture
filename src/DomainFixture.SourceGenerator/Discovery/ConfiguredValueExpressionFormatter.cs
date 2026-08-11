using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.SourceGenerator.Discovery;

internal static class ConfiguredValueExpressionFormatter
{
    public static bool TryFormat(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        out string? formatted)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                formatted = literal.ToString();
                return true;
            case ParenthesizedExpressionSyntax parenthesized:
                if (TryFormat(semanticModel, parenthesized.Expression, out var parenthesizedValue))
                {
                    formatted = $"({parenthesizedValue})";
                    return true;
                }
                break;
            case PrefixUnaryExpressionSyntax unary:
                if (TryFormat(semanticModel, unary.Operand, out var operand))
                {
                    formatted = unary.OperatorToken.Text + operand;
                    return true;
                }
                break;
            case DefaultExpressionSyntax defaultExpression:
                if (semanticModel.GetTypeInfo(defaultExpression.Type).Type is { } defaultType)
                {
                    formatted = $"default({Display(defaultType)})";
                    return true;
                }
                break;
            case ObjectCreationExpressionSyntax creation:
                if (semanticModel.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor &&
                    TryFormatArguments(semanticModel, creation.ArgumentList?.Arguments, out var constructorArguments))
                {
                    formatted = $"new {Display(constructor.ContainingType)}({constructorArguments})";
                    return true;
                }
                break;
            case InvocationExpressionSyntax invocation:
                if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol { IsStatic: true } method &&
                    TryFormatArguments(semanticModel, invocation.ArgumentList.Arguments, out var invocationArguments))
                {
                    formatted = $"{Display(method.ContainingType)}.{Escape(method.Name)}({invocationArguments})";
                    return true;
                }
                break;
            case MemberAccessExpressionSyntax:
            case IdentifierNameSyntax:
                var symbol = semanticModel.GetSymbolInfo(expression).Symbol;
                if (symbol is IFieldSymbol { IsStatic: true } field)
                {
                    formatted = $"{Display(field.ContainingType)}.{Escape(field.Name)}";
                    return true;
                }
                if (symbol is IPropertySymbol { IsStatic: true } property)
                {
                    formatted = $"{Display(property.ContainingType)}.{Escape(property.Name)}";
                    return true;
                }
                break;
        }

        formatted = null;
        return false;
    }

    private static bool TryFormatArguments(
        SemanticModel semanticModel,
        SeparatedSyntaxList<ArgumentSyntax>? arguments,
        out string? formatted)
    {
        if (arguments is null || arguments.Value.Count == 0)
        {
            formatted = string.Empty;
            return true;
        }

        var values = new string[arguments.Value.Count];
        for (var index = 0; index < arguments.Value.Count; index++)
        {
            var argument = arguments.Value[index];
            if (!argument.RefKindKeyword.IsKind(SyntaxKind.None) ||
                !TryFormat(semanticModel, argument.Expression, out var value))
            {
                formatted = null;
                return false;
            }

            values[index] = argument.NameColon is null
                ? value!
                : $"{argument.NameColon.Name.Identifier.ValueText}: {value}";
        }

        formatted = string.Join(", ", values);
        return true;
    }

    private static string Display(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string Escape(string identifier) =>
        SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
        SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;
}
