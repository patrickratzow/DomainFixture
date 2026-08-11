using System;

namespace DomainFixture.SourceGenerator.Models;

internal enum DomainCommandArgumentSource
{
    ExplicitExpression,
    AutoFixtureValue
}

internal sealed class DomainCommandArgumentSpec
{
    public int Position { get; }
    public string ParameterName { get; }
    public string TypeName { get; }
    public string ExpressionText { get; }
    public DomainCommandArgumentSource Source { get; }

    public DomainCommandArgumentSpec(
        int position,
        string parameterName,
        string typeName,
        string expressionText,
        DomainCommandArgumentSource source)
    {
        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position));
        if (string.IsNullOrWhiteSpace(parameterName))
            throw new ArgumentException("A command parameter name is required.", nameof(parameterName));
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("A command parameter type is required.", nameof(typeName));
        if (string.IsNullOrWhiteSpace(expressionText))
            throw new ArgumentException("A command argument expression is required.", nameof(expressionText));

        Position = position;
        ParameterName = parameterName;
        TypeName = typeName;
        ExpressionText = expressionText;
        Source = source;
    }
}
