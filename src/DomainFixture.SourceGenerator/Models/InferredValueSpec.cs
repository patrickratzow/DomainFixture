using System;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class InferredValueSpec
{
    public string TypeName { get; }
    public string Expression { get; }
    public string SourceId { get; }
    public Location? Location { get; }

    public InferredValueSpec(
        string typeName,
        string expression,
        string sourceId,
        Location? location)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("An inferred value type is required.", nameof(typeName));
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("An inferred value expression is required.", nameof(expression));
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("An inferred value source is required.", nameof(sourceId));

        TypeName = typeName;
        Expression = expression;
        SourceId = sourceId;
        Location = location;
    }
}
