using System;
using System.Linq;
using System.Threading;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.SourceGenerator.Extraction;

/// <summary>
/// Adapts FluentValidation syntax into framework-neutral domain constraints.
/// Boundary selection happens later and has no FluentValidation dependency.
/// </summary>
internal static class FluentValidationConstraintAdapter
{
    public static IncrementalValuesProvider<ConstraintExtractionResult> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsConstraintInvocationCandidate(node),
                static (syntaxContext, cancellationToken) =>
                    ExtractConstraint(syntaxContext, cancellationToken))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);
    }

    private static bool IsConstraintInvocationCandidate(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax memberAccess
            })
        {
            return false;
        }

        return memberAccess.Name.Identifier.ValueText is
            "Length" or "MinimumLength" or "MaximumLength" or
            "NotEmpty" or "NotNull" or
            "InclusiveBetween" or "ExclusiveBetween" or
            "GreaterThan" or "LessThan" or
            "EmailAddress" or "Matches" or "Must" or
            "Equal" or "NotEqual" or "Empty" or "Null" or
            "IsInEnum" or "CreditCard" or "PrecisionScale";
    }

    private static ConstraintExtractionResult? ExtractConstraint(
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

        var methodName = ((MemberAccessExpressionSyntax)invocation.Expression)
            .Name.Identifier.ValueText;
        if (!IsSupported(methodName))
        {
            return ConstraintExtractionResult.Failure(
                GeneratorDiagnostics.ConstraintAdapterMissing(
                    invocation.GetLocation(),
                    "FluentValidation",
                    methodName));
        }

        if (!TryGetRuleProperty(semanticModel, invocation, out var property))
        {
            return CannotInterpret(invocation, methodName, "the RuleFor target is not a property");
        }

        var validationSourceType = semanticModel.GetEnclosingSymbol(
            invocation.SpanStart,
            cancellationToken)?.ContainingType;
        var subjectType = validationSourceType is null
            ? null
            : FindValidatedType(validationSourceType);
        if (validationSourceType is null || subjectType is null)
        {
            return CannotInterpret(invocation, methodName, "the validated subject type could not be determined");
        }

        string kindId;
        int? minimum = null;
        int? maximum = null;
        switch (methodName)
        {
            case "Length":
                if (!IsString(property) ||
                    !TryReadTwoIntArguments(semanticModel, invocation, out var lengthMinimum, out var lengthMaximum) ||
                    lengthMinimum < 0 ||
                    lengthMaximum < lengthMinimum)
                {
                    return CannotInterpret(invocation, methodName, "constant non-negative string bounds are required");
                }

                kindId = DomainConstraintKinds.TextLength;
                minimum = lengthMinimum;
                maximum = lengthMaximum;
                break;

            case "MinimumLength":
                if (!IsString(property) ||
                    !TryReadSingleIntArgument(semanticModel, invocation, out var parsedMinimumLength) ||
                    parsedMinimumLength < 0)
                {
                    return CannotInterpret(invocation, methodName, "a constant non-negative string bound is required");
                }

                kindId = DomainConstraintKinds.TextMinimumLength;
                minimum = parsedMinimumLength;
                break;

            case "MaximumLength":
                if (!IsString(property) ||
                    !TryReadSingleIntArgument(semanticModel, invocation, out var parsedMaximumLength) ||
                    parsedMaximumLength < 0)
                {
                    return CannotInterpret(invocation, methodName, "a constant non-negative string bound is required");
                }

                kindId = DomainConstraintKinds.TextMaximumLength;
                maximum = parsedMaximumLength;
                break;

            case "NotEmpty":
                if (!IsString(property))
                    return CannotInterpret(invocation, methodName, "only text presence constraints are supported currently");
                kindId = DomainConstraintKinds.TextNotEmpty;
                break;

            case "NotNull":
                if (!IsString(property))
                    return CannotInterpret(invocation, methodName, "only text presence constraints are supported currently");
                kindId = DomainConstraintKinds.TextNotNull;
                break;

            case "InclusiveBetween":
                if (!IsInt32(property) ||
                    !TryReadTwoIntArguments(semanticModel, invocation, out var inclusiveMinimum, out var inclusiveMaximum) ||
                    inclusiveMaximum < inclusiveMinimum)
                {
                    return CannotInterpret(invocation, methodName, "constant Int32 bounds in ascending order are required");
                }

                kindId = DomainConstraintKinds.Int32InclusiveRange;
                minimum = inclusiveMinimum;
                maximum = inclusiveMaximum;
                break;

            case "ExclusiveBetween":
                if (!IsInt32(property) ||
                    !TryReadTwoIntArguments(semanticModel, invocation, out var exclusiveMinimum, out var exclusiveMaximum) ||
                    (long)exclusiveMaximum - exclusiveMinimum <= 1)
                {
                    return CannotInterpret(invocation, methodName, "constant Int32 bounds containing at least one valid value are required");
                }

                kindId = DomainConstraintKinds.Int32ExclusiveRange;
                minimum = exclusiveMinimum;
                maximum = exclusiveMaximum;
                break;

            case "GreaterThan":
                if (!IsInt32(property) ||
                    !TryReadSingleIntArgument(semanticModel, invocation, out var greaterThan) ||
                    greaterThan == int.MaxValue)
                {
                    return CannotInterpret(invocation, methodName, "a constant Int32 bound with a representable valid value is required");
                }

                kindId = DomainConstraintKinds.Int32GreaterThan;
                minimum = greaterThan;
                break;

            case "LessThan":
                if (!IsInt32(property) ||
                    !TryReadSingleIntArgument(semanticModel, invocation, out var lessThan) ||
                    lessThan == int.MinValue)
                {
                    return CannotInterpret(invocation, methodName, "a constant Int32 bound with a representable valid value is required");
                }

                kindId = DomainConstraintKinds.Int32LessThan;
                maximum = lessThan;
                break;

            default:
                return null;
        }

        return ConstraintExtractionResult.Success(new DiscoveredDomainConstraint(
            validationSourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            property.Name,
            kindId,
            minimum,
            maximum,
            FindErrorCode(semanticModel, invocation),
            HasAccessibleSetter(property),
            invocation.GetLocation()));
    }

    private static bool IsSupported(string methodName) => methodName is
        "Length" or "MinimumLength" or "MaximumLength" or
        "NotEmpty" or "NotNull" or
        "InclusiveBetween" or "ExclusiveBetween" or
        "GreaterThan" or "LessThan";

    private static ConstraintExtractionResult CannotInterpret(
        InvocationExpressionSyntax invocation,
        string methodName,
        string reason) =>
        ConstraintExtractionResult.Failure(
            GeneratorDiagnostics.ConstraintAdapterCouldNotInterpret(
                invocation.GetLocation(),
                "FluentValidation",
                methodName,
                reason));

    private static ITypeSymbol? FindValidatedType(INamedTypeSymbol validationRulesType)
    {
        var validatorInterface = validationRulesType.AllInterfaces.FirstOrDefault(candidate =>
            candidate.OriginalDefinition.MetadataName == "IValidator`1" &&
            candidate.ContainingNamespace.ToDisplayString() == "FluentValidation");
        if (validatorInterface?.TypeArguments.Length == 1)
            return validatorInterface.TypeArguments[0];

        for (var current = validationRulesType.BaseType; current is not null; current = current.BaseType)
        {
            if (current.OriginalDefinition.MetadataName == "AbstractValidator`1" &&
                current.ContainingNamespace.ToDisplayString() == "FluentValidation" &&
                current.TypeArguments.Length == 1)
            {
                return current.TypeArguments[0];
            }
        }

        return null;
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

    private static bool TryReadTwoIntArguments(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        out int minimum,
        out int maximum)
    {
        minimum = 0;
        maximum = 0;
        if (invocation.ArgumentList.Arguments.Count < 2)
            return false;

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
        var constant = semanticModel.GetConstantValue(invocation.ArgumentList.Arguments[index].Expression);
        if (!constant.HasValue || constant.Value is not int parsed)
            return false;

        value = parsed;
        return true;
    }

    private static bool TryGetRuleProperty(
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
            SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Body as ExpressionSyntax,
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.Body as ExpressionSyntax,
            _ => null
        };
        if (body is null || semanticModel.GetSymbolInfo(body).Symbol is not IPropertySymbol propertySymbol)
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

    private static bool HasAccessibleSetter(IPropertySymbol property) =>
        property.SetMethod?.IsInitOnly != true &&
        property.SetMethod?.DeclaredAccessibility is Accessibility.Public or
            Accessibility.Internal or
            Accessibility.ProtectedOrInternal;

    private static bool IsString(IPropertySymbol property) =>
        property.Type.SpecialType == SpecialType.System_String;

    private static bool IsInt32(IPropertySymbol property) =>
        property.Type.SpecialType == SpecialType.System_Int32;
}
