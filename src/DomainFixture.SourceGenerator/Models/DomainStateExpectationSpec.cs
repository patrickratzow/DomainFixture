using System;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class DomainStateExpectationSpec
{
    public string Name { get; }
    public string MemberPath { get; }
    public string ExpectedStateExpression { get; }
    public Location? Location { get; }

    public DomainStateExpectationSpec(
        string name,
        string memberPath,
        string expectedStateExpression,
        Location? location)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A state declaration name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("A state member path is required.", nameof(memberPath));
        if (string.IsNullOrWhiteSpace(expectedStateExpression))
            throw new ArgumentException("An expected state expression is required.", nameof(expectedStateExpression));

        Name = name;
        MemberPath = memberPath;
        ExpectedStateExpression = expectedStateExpression;
        Location = location;
    }
}
