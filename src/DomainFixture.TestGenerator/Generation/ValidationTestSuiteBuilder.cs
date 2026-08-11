using System;
using System.Collections.Generic;
using System.Linq;
using DomainFixture.TestGenerator.Model;
using DomainFixture.TestGenerator.Model.Validation;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainFixture.TestGenerator.Generation;

/// <summary>
/// Translates validation recipes and boundary cases into executable generated tests.
/// </summary>
public sealed class ValidationTestSuiteBuilder
{
    public GeneratedTestSuite Build(ValidationTestSuiteDescriptor descriptor)
    {
        if (descriptor is null) throw new ArgumentNullException(nameof(descriptor));

        var tests = new List<GeneratedTest>
        {
            new(
                $"{descriptor.RecipeName}_Baseline_IsValid",
                CreateStatements(descriptor, validationCase: null),
                isAsync: descriptor.ExecutionMode == ValidationExecutionMode.Asynchronous)
        };

        tests.AddRange(descriptor.Cases.Select(validationCase =>
            new GeneratedTest(
                $"{descriptor.RecipeName}_{validationCase.Name}",
                CreateStatements(descriptor, validationCase),
                isAsync: descriptor.ExecutionMode == ValidationExecutionMode.Asynchronous)));

        return new GeneratedTestSuite(descriptor.NamespaceName, descriptor.ClassName, tests);
    }

    private static IEnumerable<StatementSyntax> CreateStatements(
        ValidationTestSuiteDescriptor descriptor,
        GeneratedValidationCase? validationCase)
    {
        yield return CreateLocal(
            descriptor.SubjectType,
            "subject",
            descriptor.BaselineFactory);

        if (validationCase is not null)
            yield return CreateMutation(validationCase.Mutation);

        yield return CreateLocal(
            IdentifierName("var"),
            "validator",
            descriptor.ValidatorFactory);

        yield return CreateLocal(
            IdentifierName("var"),
            "report",
            CreateValidationInvocation(descriptor.ExecutionMode));

        yield return CreateAssertion(validationCase);
    }

    private static ExpressionSyntax CreateValidationInvocation(ValidationExecutionMode executionMode)
    {
        var methodName = executionMode == ValidationExecutionMode.Asynchronous
            ? "ValidateAsync"
            : "Validate";
        ExpressionSyntax invocation = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("validator"),
                    IdentifierName(methodName)))
            .AddArgumentListArguments(Argument(IdentifierName("subject")));

        if (executionMode == ValidationExecutionMode.Asynchronous)
            invocation = AwaitExpression(invocation);

        return invocation;
    }

    private static LocalDeclarationStatementSyntax CreateLocal(
        TypeSyntax type,
        string name,
        ExpressionSyntax initializer)
    {
        return LocalDeclarationStatement(
            VariableDeclaration(type)
                .AddVariables(
                    VariableDeclarator(Identifier(name))
                        .WithInitializer(EqualsValueClause(initializer))));
    }

    private static ExpressionStatementSyntax CreateMutation(PropertyMutationDescriptor mutation)
    {
        if (mutation.ReconstructionFactory is not null)
        {
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("subject"),
                    InvocationExpression(mutation.ReconstructionFactory)
                        .AddArgumentListArguments(
                            Argument(IdentifierName("subject")),
                            Argument(mutation.Value))));
        }

        return ExpressionStatement(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("subject"),
                    IdentifierName(mutation.Property.Name)),
                mutation.Value));
    }

    private static ExpressionStatementSyntax CreateAssertion(GeneratedValidationCase? validationCase)
    {
        ExpressionSyntax actual;

        if (validationCase is null || validationCase.ExpectedOutcome == ExpectedValidationOutcome.Valid)
        {
            actual = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("report"),
                IdentifierName("IsValid"));
        }
        else
        {
            var containsFailure = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("report"),
                        IdentifierName("ContainsFailure")))
                .AddArgumentListArguments(
                    Argument(CreateStringLiteral(validationCase.Mutation.Property.Name)));

            if (validationCase.ErrorCode is not null)
            {
                containsFailure = containsFailure.AddArgumentListArguments(
                    Argument(CreateStringLiteral(validationCase.ErrorCode)));
            }

            actual = containsFailure;
        }

        return ExpressionStatement(
            InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Assert"),
                        IdentifierName("That")))
                .AddArgumentListArguments(
                    Argument(actual),
                    Argument(MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Is"),
                        IdentifierName("True")))));
    }

    private static LiteralExpressionSyntax CreateStringLiteral(string value)
    {
        return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value));
    }
}
