using System.Collections.Generic;
using System.Linq;
using System.Text;
using DomainFixture.Contracts;
using DomainFixture.Pipeline;
using DomainFixture.SourceGenerator.Models;
using DomainFixture.TestGenerator.Model;
using Microsoft.CodeAnalysis.CSharp;

namespace DomainFixture.SourceGenerator.Generation;

internal static class DomainScenarioProviderPipeline
{
    private static readonly ProviderPipeline<
        DomainScenarioPlanningRequest,
        IReadOnlyList<GeneratedTest>> Pipeline = new(
        new IPipelineProvider<DomainScenarioPlanningRequest, IReadOnlyList<GeneratedTest>>[]
        {
            new ValueObjectEqualityScenarioProvider(),
            new ConstructionRoundTripScenarioProvider(),
            new StateTransitionScenarioProvider(),
            new RejectedTransitionScenarioProvider(),
            new StateExpectationScenarioProvider()
        },
        ProviderPipelineMode.ExactlyOne);

    public static ProviderResolution<IReadOnlyList<GeneratedTest>> Resolve(
        DomainScenarioPlanningRequest request) => Pipeline.Resolve(request);
}

internal abstract class DomainTransitionScenarioProviderBase
{
    protected static ProviderDecision<IReadOnlyList<GeneratedTest>> Invalid(string reason) =>
        ProviderDecision<IReadOnlyList<GeneratedTest>>.Invalid(reason);

    protected static bool TryResolveTransition(
        DomainScenarioPlanningRequest request,
        bool isRejection,
        out DomainTransitionSpec? transition,
        out string? failureReason)
    {
        transition = null;
        failureReason = null;
        if (!request.Contract.Parameters.TryGetValue(
                DomainTransitionScenarioContractFactory.ScenarioNameParameter,
                out var scenarioName) ||
            string.IsNullOrWhiteSpace(scenarioName))
        {
            failureReason = "a transition scenario requires a non-empty scenario name";
            return false;
        }

        var matches = request.Configuration.Transitions
            .Where(candidate =>
                candidate.IsRejection == isRejection &&
                candidate.Name == scenarioName)
            .ToArray();
        if (matches.Length != 1)
        {
            failureReason = matches.Length == 0
                ? $"transition '{scenarioName}' was not declared by the fixture recipe"
                : $"transition '{scenarioName}' is declared more than once by the fixture recipe";
            return false;
        }

        transition = matches[0];
        return true;
    }

    protected static string CreateTransitionIdentifier(string name)
    {
        var builder = new StringBuilder(name.Length + 1);
        foreach (var character in name)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        if (builder.Length == 0 || !char.IsLetter(builder[0]) && builder[0] != '_')
            builder.Insert(0, '_');

        return builder.ToString();
    }

    protected static bool TryCreateCommandInvocation(
        DomainScenarioPlanningRequest request,
        DomainTransitionSpec transition,
        out string? invocation,
        out string? failureReason)
    {
        return DomainTransitionInvocationPlanner.TryCreate(
            transition,
            request.Constraints,
            request.NestedResolver,
            request.ConfiguredValues,
            request.InferredValues,
            out invocation,
            out failureReason);
    }
}

internal sealed class StateTransitionScenarioProvider :
    DomainTransitionScenarioProviderBase,
    IPipelineProvider<DomainScenarioPlanningRequest, IReadOnlyList<GeneratedTest>>
{
    public string Id => "domainfixture.scenarios.state-transition";

    public ProviderDecision<IReadOnlyList<GeneratedTest>> Evaluate(
        DomainScenarioPlanningRequest request)
    {
        if (request.Contract.KindId != DomainTransitionScenarioKinds.StateTransition)
            return ProviderDecision<IReadOnlyList<GeneratedTest>>.NotHandled();
        if (!TryResolveTransition(
                request,
                isRejection: false,
                out var transition,
                out var failureReason))
        {
            return Invalid(failureReason!);
        }
        if (transition is null)
            return Invalid("the transition could not be resolved");

        if (!TryCreateCommandInvocation(
                request,
                transition,
                out var invocation,
                out failureReason))
            return Invalid(failureReason!);

        if (transition.ExecutionKind != DomainTransitionExecutionKind.ResultCommand &&
            (transition.StateMemberPath is null || transition.ExpectedStateExpression is null))
        {
            return Invalid("a state transition requires a readable state member and expected state");
        }

        var configuration = request.Configuration;
        var subjectType = configuration.SubjectTypeName;
        var baseline = configuration.BaselineFactoryExpression;
        var transitionIdentifier = CreateTransitionIdentifier(transition.Name);
        var testPrefix = $"{configuration.RecipeName}_Transition_{transitionIdentifier}";
        var executionStatement = transition.ExecutionKind switch
        {
            DomainTransitionExecutionKind.MutatingCommand => $"{invocation};",
            DomainTransitionExecutionKind.ImmutableCommand => $"subject = {invocation};",
            DomainTransitionExecutionKind.ResultCommand => $"var result = {invocation};",
            _ => $"{invocation};"
        };
        var primaryStatements = new List<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>
        {
            SyntaxFactory.ParseStatement($"{subjectType} subject = {baseline};"),
            SyntaxFactory.ParseStatement(executionStatement)
        };
        if (transition.ExecutionKind == DomainTransitionExecutionKind.ResultCommand)
        {
            primaryStatements.Add(SyntaxFactory.ParseStatement(
                $"global::System.Func<{transition.ResultTypeName}, bool> predicate = {transition.ResultPredicateExpression};"));
            primaryStatements.Add(SyntaxFactory.ParseStatement(
                "global::NUnit.Framework.Assert.That(predicate(result), global::NUnit.Framework.Is.True);"));
        }
        else
        {
            primaryStatements.Add(SyntaxFactory.ParseStatement(
                $"global::NUnit.Framework.Assert.That(subject.{transition.StateMemberPath}, global::NUnit.Framework.Is.EqualTo({transition.ExpectedStateExpression}));"));
        }
        var tests = new List<GeneratedTest>
        {
            new(
                $"{testPrefix}_ReachesExpectedState",
                primaryStatements)
        };

        if (request.IdentityMemberPath is not null &&
            transition.ExecutionKind != DomainTransitionExecutionKind.ResultCommand)
        {
            tests.Add(new GeneratedTest(
                $"{testPrefix}_PreservesIdentity",
                new[]
                {
                    SyntaxFactory.ParseStatement($"{subjectType} subject = {baseline};"),
                    SyntaxFactory.ParseStatement(
                        $"var identity = subject.{request.IdentityMemberPath};"),
                    SyntaxFactory.ParseStatement(executionStatement),
                    SyntaxFactory.ParseStatement(
                        $"global::NUnit.Framework.Assert.That(subject.{request.IdentityMemberPath}, global::NUnit.Framework.Is.EqualTo(identity));")
                }));
        }

        return ProviderDecision<IReadOnlyList<GeneratedTest>>.Handled(tests);
    }
}

internal sealed class RejectedTransitionScenarioProvider :
    DomainTransitionScenarioProviderBase,
    IPipelineProvider<DomainScenarioPlanningRequest, IReadOnlyList<GeneratedTest>>
{
    public string Id => "domainfixture.scenarios.rejected-transition";

    public ProviderDecision<IReadOnlyList<GeneratedTest>> Evaluate(
        DomainScenarioPlanningRequest request)
    {
        if (request.Contract.KindId != DomainTransitionScenarioKinds.RejectedTransition)
            return ProviderDecision<IReadOnlyList<GeneratedTest>>.NotHandled();
        if (!TryResolveTransition(
                request,
                isRejection: true,
                out var transition,
                out var failureReason))
        {
            return Invalid(failureReason!);
        }

        if (transition!.RejectionExceptionTypeName is null)
            return Invalid("a rejected transition requires an exception type");

        var configuration = request.Configuration;
        if (!TryCreateCommandInvocation(
                request,
                transition,
                out var invocation,
                out failureReason))
            return Invalid(failureReason!);
        var transitionIdentifier = CreateTransitionIdentifier(transition.Name);
        IReadOnlyList<GeneratedTest> tests = new[]
        {
            new GeneratedTest(
                $"{configuration.RecipeName}_Transition_{transitionIdentifier}_IsRejected",
                new[]
                {
                    SyntaxFactory.ParseStatement(
                        $"{configuration.SubjectTypeName} subject = {configuration.BaselineFactoryExpression};"),
                    SyntaxFactory.ParseStatement(
                        $"global::NUnit.Framework.Assert.Throws<{transition.RejectionExceptionTypeName}>(() => {invocation});")
                })
        };
        return ProviderDecision<IReadOnlyList<GeneratedTest>>.Handled(tests);
    }
}

internal sealed class StateExpectationScenarioProvider :
    DomainTransitionScenarioProviderBase,
    IPipelineProvider<DomainScenarioPlanningRequest, IReadOnlyList<GeneratedTest>>
{
    public string Id => "domainfixture.scenarios.state-expectation";

    public ProviderDecision<IReadOnlyList<GeneratedTest>> Evaluate(
        DomainScenarioPlanningRequest request)
    {
        if (request.Contract.KindId != DomainTransitionScenarioKinds.StateExpectation)
            return ProviderDecision<IReadOnlyList<GeneratedTest>>.NotHandled();
        if (!request.Contract.Parameters.TryGetValue(
                DomainTransitionScenarioContractFactory.ScenarioNameParameter,
                out var stateName))
            return Invalid("a state expectation requires a state name");

        var matches = request.Configuration.StateExpectations
            .Where(state => state.Name == stateName)
            .ToArray();
        if (matches.Length != 1)
            return Invalid($"state expectation '{stateName}' was not declared exactly once");

        var state = matches[0];
        IReadOnlyList<GeneratedTest> tests = new[]
        {
            new GeneratedTest(
                $"{request.Configuration.RecipeName}_State_{CreateTransitionIdentifier(state.Name)}_Matches",
                new[]
                {
                    SyntaxFactory.ParseStatement(
                        $"{request.Configuration.SubjectTypeName} subject = {request.Configuration.BaselineFactoryExpression};"),
                    SyntaxFactory.ParseStatement(
                        $"global::NUnit.Framework.Assert.That(subject.{state.MemberPath}, global::NUnit.Framework.Is.EqualTo({state.ExpectedStateExpression}));")
                })
        };
        return ProviderDecision<IReadOnlyList<GeneratedTest>>.Handled(tests);
    }
}

internal sealed class DomainScenarioPlanningRequest
{
    public DomainScenarioContract Contract { get; }
    public FixtureGenerationSpec Configuration { get; }
    public string? EquivalentCopyExpression { get; }
    public IReadOnlyList<DomainOperationContract> Operations { get; }
    public string? IdentityMemberPath { get; }
    public IReadOnlyList<DomainConstraintContract> Constraints { get; }
    public IReadOnlyList<DomainOperationOutcomeContract> Outcomes { get; }
    public IReadOnlyList<ConfiguredValueSpec> ConfiguredValues { get; }
    public IReadOnlyList<InferredValueSpec> InferredValues { get; }
    public System.Func<string, NestedValidInstanceResolution> NestedResolver { get; }

    public DomainScenarioPlanningRequest(
        DomainScenarioContract contract,
        FixtureGenerationSpec configuration,
        string? equivalentCopyExpression,
        IReadOnlyList<DomainOperationContract>? operations = null,
        string? identityMemberPath = null,
        IReadOnlyList<DomainConstraintContract>? constraints = null,
        IReadOnlyList<DomainOperationOutcomeContract>? outcomes = null,
        IReadOnlyList<ConfiguredValueSpec>? configuredValues = null,
        IReadOnlyList<InferredValueSpec>? inferredValues = null,
        System.Func<string, NestedValidInstanceResolution>? nestedResolver = null)
    {
        Contract = contract;
        Configuration = configuration;
        EquivalentCopyExpression = equivalentCopyExpression;
        Operations = operations ?? configuration.ConstructionOperations;
        IdentityMemberPath = identityMemberPath ?? configuration.IdentityMemberPath;
        Constraints = constraints ?? new DomainConstraintContract[0];
        Outcomes = outcomes ?? new DomainOperationOutcomeContract[0];
        ConfiguredValues = configuredValues ?? new ConfiguredValueSpec[0];
        InferredValues = inferredValues ?? configuration.InferredValues;
        NestedResolver = nestedResolver ?? (typeName =>
            NestedValidInstanceResolution.Uncovered(
                $"no nested recipe factory is available for '{typeName}'"));
    }
}

internal sealed class ConstructionRoundTripScenarioProvider :
    IPipelineProvider<DomainScenarioPlanningRequest, IReadOnlyList<GeneratedTest>>
{
    public string Id => "domainfixture.scenarios.construction-round-trip";

    public ProviderDecision<IReadOnlyList<GeneratedTest>> Evaluate(
        DomainScenarioPlanningRequest request)
    {
        if (request.Contract.KindId != DomainScenarioKinds.ConstructionRoundTrip)
            return ProviderDecision<IReadOnlyList<GeneratedTest>>.NotHandled();

        var operations = request.Operations;
        if (operations.Count == 0)
        {
            return ProviderDecision<IReadOnlyList<GeneratedTest>>.Invalid(
                "a construction scenario requires at least one mapped constructor or static factory");
        }

        var tests = new List<GeneratedTest>();
        foreach (var operation in operations)
        {
            var invocation = CreateInvocation(operation);
            if (invocation is null)
            {
                return ProviderDecision<IReadOnlyList<GeneratedTest>>.Invalid(
                    $"operation kind '{operation.KindId}' cannot be invoked by the construction provider");
            }

            var configuration = request.Configuration;
            var subjectType = configuration.SubjectTypeName;
            var baseline = configuration.BaselineFactoryExpression;
            var operationName = CreateOperationIdentifier(operation);
            var resultOutcome = request.Outcomes.SingleOrDefault(outcome =>
                outcome.OperationId == operation.OperationId &&
                outcome.KindId == DomainOperationOutcomeKinds.ReturnsResult);
            if (operation.ReturnTypeName != operation.SubjectTypeName && resultOutcome is null)
            {
                return ProviderDecision<IReadOnlyList<GeneratedTest>>.Invalid(
                    $"operation '{operation.OperationId}' returns a wrapper without a configured result outcome");
            }
            var successStatements = new List<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>
            {
                SyntaxFactory.ParseStatement($"{subjectType} baseline = {baseline};")
            };
            if (resultOutcome is null)
            {
                successStatements.Add(SyntaxFactory.ParseStatement(
                    $"{subjectType} constructed = {invocation};"));
            }
            else
            {
                successStatements.Add(SyntaxFactory.ParseStatement($"var result = {invocation};"));
                successStatements.Add(SyntaxFactory.ParseStatement(
                    $"global::NUnit.Framework.Assert.That(result.{resultOutcome.Parameters[0]}, global::NUnit.Framework.Is.True);"));
                successStatements.Add(SyntaxFactory.ParseStatement(
                    $"{subjectType} constructed = result.{resultOutcome.Parameters[1]};"));
            }
            successStatements.Add(SyntaxFactory.ParseStatement(
                "global::NUnit.Framework.Assert.That(constructed, global::NUnit.Framework.Is.Not.Null);"));
            tests.Add(new GeneratedTest(
                $"{configuration.RecipeName}_Construction_{operationName}_BaselineSucceeds",
                successStatements));

            var roundTripStatements = new List<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>
            {
                SyntaxFactory.ParseStatement($"{subjectType} baseline = {baseline};")
            };
            if (resultOutcome is null)
            {
                roundTripStatements.Add(SyntaxFactory.ParseStatement(
                    $"{subjectType} constructed = {invocation};"));
            }
            else
            {
                roundTripStatements.Add(SyntaxFactory.ParseStatement($"var result = {invocation};"));
                roundTripStatements.Add(SyntaxFactory.ParseStatement(
                    $"global::NUnit.Framework.Assert.That(result.{resultOutcome.Parameters[0]}, global::NUnit.Framework.Is.True);"));
                roundTripStatements.Add(SyntaxFactory.ParseStatement(
                    $"{subjectType} constructed = result.{resultOutcome.Parameters[1]};"));
            }
            roundTripStatements.AddRange(operation.Parameters.Select(parameter =>
                SyntaxFactory.ParseStatement(
                    $"global::NUnit.Framework.Assert.That(constructed.{parameter.MemberPath}, global::NUnit.Framework.Is.EqualTo(baseline.{parameter.MemberPath}));")));
            tests.Add(new GeneratedTest(
                $"{configuration.RecipeName}_Construction_{operationName}_ArgumentsRoundTrip",
                roundTripStatements));
        }

        return ProviderDecision<IReadOnlyList<GeneratedTest>>.Handled(tests);
    }

    private static string? CreateInvocation(DomainOperationContract operation)
    {
        var arguments = string.Join(", ", operation.Parameters.Select(parameter =>
            $"baseline.{parameter.MemberPath}"));
        return operation.KindId switch
        {
            DomainOperationKinds.Constructor => $"new {operation.DeclaringTypeName}({arguments})",
            DomainOperationKinds.StaticFactory =>
                $"{operation.DeclaringTypeName}.{operation.MemberName}({arguments})",
            _ => null
        };
    }

    private static string CreateOperationIdentifier(DomainOperationContract operation)
    {
        var builder = new StringBuilder(
            operation.KindId == DomainOperationKinds.Constructor
                ? "Constructor"
                : SanitizeIdentifier(operation.MemberName));
        foreach (var parameter in operation.Parameters)
        {
            builder.Append('_');
            builder.Append(SanitizeIdentifier(parameter.MemberPath));
        }

        return builder.ToString();
    }

    private static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');

        return builder.ToString();
    }
}

internal sealed class ValueObjectEqualityScenarioProvider :
    IPipelineProvider<DomainScenarioPlanningRequest, IReadOnlyList<GeneratedTest>>
{
    public string Id => "domainfixture.scenarios.value-object-equality";

    public ProviderDecision<IReadOnlyList<GeneratedTest>> Evaluate(
        DomainScenarioPlanningRequest request)
    {
        if (request.Contract.KindId != DomainScenarioKinds.ValueObjectEquality)
            return ProviderDecision<IReadOnlyList<GeneratedTest>>.NotHandled();

        if (request.EquivalentCopyExpression is null)
        {
            return ProviderDecision<IReadOnlyList<GeneratedTest>>.Invalid(
                "an equality law requires a state-preserving reconstruction strategy");
        }

        var configuration = request.Configuration;
        var subjectType = configuration.SubjectTypeName;
        var baseline = configuration.BaselineFactoryExpression;
        var equivalentCopy = request.EquivalentCopyExpression;
        var prefix = configuration.RecipeName;
        IReadOnlyList<GeneratedTest> tests = new[]
        {
            new GeneratedTest(
                $"{prefix}_Equality_IsReflexive",
                new[]
                {
                    SyntaxFactory.ParseStatement($"{subjectType} subject = {baseline};"),
                    SyntaxFactory.ParseStatement(
                        "global::NUnit.Framework.Assert.That(subject.Equals(subject), global::NUnit.Framework.Is.True);")
                }),
            new GeneratedTest(
                $"{prefix}_Equality_EquivalentValuesAreSymmetric",
                new[]
                {
                    SyntaxFactory.ParseStatement($"{subjectType} subject = {baseline};"),
                    SyntaxFactory.ParseStatement(
                        $"{subjectType} equivalent = {equivalentCopy};"),
                    SyntaxFactory.ParseStatement(
                        "global::NUnit.Framework.Assert.That(subject.Equals(equivalent), global::NUnit.Framework.Is.True);"),
                    SyntaxFactory.ParseStatement(
                        "global::NUnit.Framework.Assert.That(equivalent.Equals(subject), global::NUnit.Framework.Is.True);")
                }),
            new GeneratedTest(
                $"{prefix}_Equality_EquivalentValuesHaveSameHashCode",
                new[]
                {
                    SyntaxFactory.ParseStatement($"{subjectType} subject = {baseline};"),
                    SyntaxFactory.ParseStatement(
                        $"{subjectType} equivalent = {equivalentCopy};"),
                    SyntaxFactory.ParseStatement(
                        "global::NUnit.Framework.Assert.That(subject.GetHashCode(), global::NUnit.Framework.Is.EqualTo(equivalent.GetHashCode()));")
                })
        };
        return ProviderDecision<IReadOnlyList<GeneratedTest>>.Handled(tests);
    }
}
