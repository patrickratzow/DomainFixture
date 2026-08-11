using System.Collections.Immutable;
using DomainFixture.Contracts;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal enum DomainTransitionExecutionKind
{
    MutatingCommand,
    ImmutableCommand,
    ResultCommand
}

internal sealed class DomainTransitionSpec
{
    public string Name { get; }
    public DomainOperationContract Operation { get; }
    public string? StateMemberPath { get; }
    public string? ExpectedStateExpression { get; }
    public string? RejectionExceptionTypeName { get; }
    public Location? Location { get; }
    public ImmutableArray<DomainCommandArgumentSpec> Arguments { get; }
    public DomainTransitionExecutionKind ExecutionKind { get; }
    public string? StateName { get; }
    public string? ResultTypeName { get; }
    public string? ResultPredicateExpression { get; }

    public bool IsRejection => RejectionExceptionTypeName is not null;

    public DomainTransitionSpec(
        string name,
        DomainOperationContract operation,
        string? stateMemberPath,
        string? expectedStateExpression,
        string? rejectionExceptionTypeName,
        Location? location)
        : this(
            name,
            operation,
            ImmutableArray<DomainCommandArgumentSpec>.Empty,
            DomainTransitionExecutionKind.MutatingCommand,
            stateName: null,
            stateMemberPath,
            expectedStateExpression,
            resultTypeName: null,
            resultPredicateExpression: null,
            rejectionExceptionTypeName,
            location)
    {
    }

    public DomainTransitionSpec(
        string name,
        DomainOperationContract operation,
        ImmutableArray<DomainCommandArgumentSpec> arguments,
        DomainTransitionExecutionKind executionKind,
        string? stateName,
        string? stateMemberPath,
        string? expectedStateExpression,
        string? resultTypeName,
        string? resultPredicateExpression,
        string? rejectionExceptionTypeName,
        Location? location)
    {
        Name = name;
        Operation = operation;
        Arguments = arguments.IsDefault
            ? ImmutableArray<DomainCommandArgumentSpec>.Empty
            : arguments;
        ExecutionKind = executionKind;
        StateName = stateName;
        StateMemberPath = stateMemberPath;
        ExpectedStateExpression = expectedStateExpression;
        ResultTypeName = resultTypeName;
        ResultPredicateExpression = resultPredicateExpression;
        RejectionExceptionTypeName = rejectionExceptionTypeName;
        Location = location;
    }
}
