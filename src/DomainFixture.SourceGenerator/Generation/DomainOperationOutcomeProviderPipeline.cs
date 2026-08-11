using System.Collections.Generic;
using System.Linq;
using System.Text;
using DomainFixture.Contracts;
using DomainFixture.Pipeline;
using DomainFixture.TestGenerator.Model;
using DomainFixture.TestGenerator.Model.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DomainFixture.SourceGenerator.Generation;

internal static class DomainOperationOutcomeProviderPipeline
{
    private static readonly ProviderPipeline<
        OperationRejectionPlanningRequest,
        GeneratedTest> Pipeline = new(
        new IPipelineProvider<OperationRejectionPlanningRequest, GeneratedTest>[]
        {
            new SynchronousExceptionOutcomeProvider(),
            new SynchronousResultOutcomeProvider()
        },
        ProviderPipelineMode.ExactlyOne);

    public static ProviderResolution<GeneratedTest> Resolve(
        OperationRejectionPlanningRequest request) => Pipeline.Resolve(request);
}

internal sealed class OperationRejectionPlanningRequest
{
    public DomainOperationContract Operation { get; }
    public DomainOperationOutcomeContract Outcome { get; }
    public string BaselineFactoryExpression { get; }
    public GeneratedValidationCase InvalidCase { get; }

    public OperationRejectionPlanningRequest(
        DomainOperationContract operation,
        DomainOperationOutcomeContract outcome,
        string baselineFactoryExpression,
        GeneratedValidationCase invalidCase)
    {
        Operation = operation;
        Outcome = outcome;
        BaselineFactoryExpression = baselineFactoryExpression;
        InvalidCase = invalidCase;
    }
}

internal sealed class SynchronousResultOutcomeProvider :
    IPipelineProvider<OperationRejectionPlanningRequest, GeneratedTest>
{
    public string Id => "domainfixture.outcomes.synchronous-result";

    public ProviderDecision<GeneratedTest> Evaluate(OperationRejectionPlanningRequest request)
    {
        if (request.Outcome.KindId != DomainOperationOutcomeKinds.ReturnsResult)
            return ProviderDecision<GeneratedTest>.NotHandled();
        if (request.Outcome.OperationId != request.Operation.OperationId)
            return ProviderDecision<GeneratedTest>.Invalid(
                "the outcome operation identifier does not match the operation being invoked");
        if (request.InvalidCase.ExpectedOutcome != ExpectedValidationOutcome.Invalid)
            return ProviderDecision<GeneratedTest>.Invalid(
                "a result rejection test requires an invalid boundary case");

        var targetMemberPath = request.InvalidCase.Mutation.Property.Name;
        var matches = request.Operation.Parameters
            .Where(parameter => parameter.MemberPath == targetMemberPath)
            .ToArray();
        if (matches.Length != 1)
            return ProviderDecision<GeneratedTest>.Invalid(
                $"operation '{request.Operation.OperationId}' must map exactly one parameter to '{targetMemberPath}'");
        if (request.Operation.KindId != DomainOperationKinds.StaticFactory)
            return ProviderDecision<GeneratedTest>.Invalid(
                "a generic result outcome currently requires a synchronous static factory");

        var arguments = string.Join(", ", request.Operation.Parameters.Select(parameter =>
            parameter.Name == matches[0].Name
                ? request.InvalidCase.Mutation.Value.NormalizeWhitespace().ToFullString()
                : $"baseline.{parameter.MemberPath}"));
        var invocation =
            $"{request.Operation.DeclaringTypeName}.{request.Operation.MemberName}({arguments})";
        var statements = new[]
        {
            SyntaxFactory.ParseStatement(
                $"{request.Operation.SubjectTypeName} baseline = {request.BaselineFactoryExpression};"),
            SyntaxFactory.ParseStatement($"var result = {invocation};"),
            SyntaxFactory.ParseStatement(
                $"global::NUnit.Framework.Assert.That(result.{request.Outcome.Parameters[0]}, global::NUnit.Framework.Is.False);")
        };
        return ProviderDecision<GeneratedTest>.Handled(new GeneratedTest(
            $"Construction_{Sanitize(request.Operation.MemberName)}_{Sanitize(request.InvalidCase.Name)}_ReturnsFailure",
            statements));
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        return builder.ToString();
    }
}

internal sealed class SynchronousExceptionOutcomeProvider :
    IPipelineProvider<OperationRejectionPlanningRequest, GeneratedTest>
{
    public string Id => "domainfixture.outcomes.synchronous-exception";

    public ProviderDecision<GeneratedTest> Evaluate(OperationRejectionPlanningRequest request)
    {
        if (request.Outcome.KindId != DomainOperationOutcomeKinds.ThrowsException)
            return ProviderDecision<GeneratedTest>.NotHandled();

        if (request.Outcome.OperationId != request.Operation.OperationId)
        {
            return ProviderDecision<GeneratedTest>.Invalid(
                "the outcome operation identifier does not match the operation being invoked");
        }

        if (request.InvalidCase.ExpectedOutcome != ExpectedValidationOutcome.Invalid)
        {
            return ProviderDecision<GeneratedTest>.Invalid(
                "an operation rejection test requires an invalid boundary case");
        }

        var targetMemberPath = request.InvalidCase.Mutation.Property.Name;
        var matchingParameters = request.Operation.Parameters
            .Where(parameter => parameter.MemberPath == targetMemberPath)
            .ToArray();
        if (matchingParameters.Length == 0)
        {
            return ProviderDecision<GeneratedTest>.Invalid(
                $"operation '{request.Operation.OperationId}' has no parameter mapped to '{targetMemberPath}'");
        }

        if (matchingParameters.Length > 1)
        {
            return ProviderDecision<GeneratedTest>.Invalid(
                $"operation '{request.Operation.OperationId}' maps multiple parameters to '{targetMemberPath}'");
        }

        var invocation = CreateInvocation(request, matchingParameters[0].Name);
        if (invocation is null)
        {
            return ProviderDecision<GeneratedTest>.Invalid(
                $"operation kind '{request.Operation.KindId}' is not a synchronous construction operation");
        }

        var exceptionType = request.Outcome.Parameters[0];
        var statements = new[]
        {
            SyntaxFactory.ParseStatement(
                $"{request.Operation.SubjectTypeName} baseline = {request.BaselineFactoryExpression};"),
            SyntaxFactory.ParseStatement(
                $"global::NUnit.Framework.Assert.Throws<{exceptionType}>(() => {invocation});")
        };
        return ProviderDecision<GeneratedTest>.Handled(new GeneratedTest(
            $"Construction_{SanitizeIdentifier(request.Operation.MemberName)}_{SanitizeIdentifier(request.InvalidCase.Name)}_IsRejected",
            statements));
    }

    private static string? CreateInvocation(
        OperationRejectionPlanningRequest request,
        string targetedParameterName)
    {
        var operation = request.Operation;
        var arguments = string.Join(", ", operation.Parameters.Select(parameter =>
            parameter.Name == targetedParameterName
                ? request.InvalidCase.Mutation.Value.NormalizeWhitespace().ToFullString()
                : $"baseline.{parameter.MemberPath}"));

        return operation.KindId switch
        {
            DomainOperationKinds.Constructor => $"new {operation.DeclaringTypeName}({arguments})",
            DomainOperationKinds.StaticFactory =>
                $"{operation.DeclaringTypeName}.{operation.MemberName}({arguments})",
            _ => null
        };
    }

    private static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');

        return builder.ToString();
    }
}
