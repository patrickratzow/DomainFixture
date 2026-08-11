using System;
using System.Linq;
using System.Threading;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.SourceGenerator.Extraction;

internal static class FluentValidationRuleProvider
{
    public static IncrementalValuesProvider<RuleExtractionResult> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsSupportedInvocationCandidate(node),
                static (syntaxContext, cancellationToken) =>
                    ExtractRule(syntaxContext, cancellationToken))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);
    }

    private static bool IsSupportedInvocationCandidate(SyntaxNode node)
    {
        return node is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Length" or "MaximumLength" or "NotEmpty" or "NotNull"
            }
        };
    }

    private static RuleExtractionResult? ExtractRule(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var method = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        if (method?.ContainingNamespace.ToDisplayString()
                .StartsWith("FluentValidation", StringComparison.Ordinal) != true)
        {
            return null;
        }

        if (!TryGetRuleProperty(semanticModel, invocation, out var property))
        {
            return null;
        }

        var methodName = ((MemberAccessExpressionSyntax)invocation.Expression).Name.Identifier.ValueText;
        var kind = methodName switch
        {
            "Length" => ValidationRuleKind.StringLength,
            "MaximumLength" => ValidationRuleKind.StringMaximumLength,
            "NotEmpty" => ValidationRuleKind.NotEmpty,
            "NotNull" => ValidationRuleKind.NotNull,
            _ => (ValidationRuleKind?)null
        };
        if (kind is null)
            return null;

        int? minimum = null;
        int? maximum = null;
        if (kind == ValidationRuleKind.StringLength)
        {
            if (!TryReadLengthArguments(semanticModel, invocation, out var parsedMinimum, out var parsedMaximum) ||
                parsedMinimum < 0 ||
                parsedMaximum < parsedMinimum)
            {
                return null;
            }

            minimum = parsedMinimum;
            maximum = parsedMaximum;
        }
        else if (kind == ValidationRuleKind.StringMaximumLength)
        {
            if (!TryReadSingleIntArgument(semanticModel, invocation, out var parsedMaximum) ||
                parsedMaximum < 0)
            {
                return null;
            }

            maximum = parsedMaximum;
        }

        var validationRulesType = semanticModel.GetEnclosingSymbol(
            invocation.SpanStart,
            cancellationToken)?.ContainingType;
        if (validationRulesType is null)
            return null;

        return RuleExtractionResult.Success(new ValidationRuleSpec(
            validationRulesType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            property.Name,
            kind.Value,
            minimum,
            maximum,
            FindErrorCode(semanticModel, invocation),
            HasAccessibleSetter(property),
            invocation.GetLocation()));
    }

    private static bool TryReadSingleIntArgument(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        out int value)
    {
        value = 0;
        if (invocation.ArgumentList.Arguments.Count < 1)
            return false;

        var constant = semanticModel.GetConstantValue(invocation.ArgumentList.Arguments[0].Expression);
        if (!constant.HasValue || constant.Value is not int parsed)
            return false;

        value = parsed;
        return true;
    }

    private static bool TryReadLengthArguments(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        out int minimum,
        out int maximum)
    {
        minimum = 0;
        maximum = 0;
        if (invocation.ArgumentList.Arguments.Count < 2)
            return false;

        var minimumValue = semanticModel.GetConstantValue(invocation.ArgumentList.Arguments[0].Expression);
        var maximumValue = semanticModel.GetConstantValue(invocation.ArgumentList.Arguments[1].Expression);
        if (!minimumValue.HasValue || minimumValue.Value is not int parsedMinimum ||
            !maximumValue.HasValue || maximumValue.Value is not int parsedMaximum)
        {
            return false;
        }

        minimum = parsedMinimum;
        maximum = parsedMaximum;
        return true;
    }

    private static bool TryGetRuleProperty(
        SemanticModel semanticModel,
        InvocationExpressionSyntax validationInvocation,
        out IPropertySymbol property)
    {
        property = null!;
        var ruleForInvocation = FindRuleForInvocation(validationInvocation);
        if (ruleForInvocation is null ||
            ruleForInvocation.ArgumentList.Arguments.Count == 0)
        {
            return false;
        }

        ExpressionSyntax? body = ruleForInvocation.ArgumentList.Arguments[0].Expression switch
        {
            SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Body as ExpressionSyntax,
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.Body as ExpressionSyntax,
            _ => null
        };
        if (body is null)
            return false;

        var propertySymbol = semanticModel.GetSymbolInfo(body).Symbol as IPropertySymbol;
        if (propertySymbol is null || propertySymbol.Type.SpecialType != SpecialType.System_String)
            return false;

        property = propertySymbol;
        return true;
    }

    private static InvocationExpressionSyntax? FindRuleForInvocation(
        InvocationExpressionSyntax validationInvocation)
    {
        var current = validationInvocation;
        while (current.Expression is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Expression is InvocationExpressionSyntax previous)
        {
            if (IsRuleForInvocation(previous))
                return previous;

            current = previous;
        }

        return null;
    }

    private static bool IsRuleForInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "RuleFor",
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText == "RuleFor",
            _ => false
        };
    }

    private static string? FindErrorCode(
        SemanticModel semanticModel,
        InvocationExpressionSyntax validationInvocation)
    {
        if (validationInvocation.Parent is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression == validationInvocation &&
            memberAccess.Name.Identifier.ValueText == "WithErrorCode" &&
            memberAccess.Parent is InvocationExpressionSyntax outerInvocation &&
            outerInvocation.ArgumentList.Arguments.Count > 0)
        {
            var value = semanticModel.GetConstantValue(
                outerInvocation.ArgumentList.Arguments[0].Expression);
            return value.HasValue ? value.Value as string : null;
        }

        return null;
    }

    private static bool HasAccessibleSetter(IPropertySymbol property)
    {
        return property.SetMethod?.DeclaredAccessibility is Accessibility.Public or
            Accessibility.Internal or
            Accessibility.ProtectedOrInternal;
    }
}
