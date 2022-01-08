using System;
using System.Collections.Generic;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
using System.Linq.Expressions;

namespace DomainFixture.FixtureConfigurations;

internal static class PropertyExpressionService 
{
    internal static string GetName<TClass, TProperty>(Expression<Func<TClass, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body.NodeType is not ExpressionType.MemberAccess)
            throw new InvalidOperationException($"{propertyExpression} is not a direct property expression");

        var name = propertyExpression.Body.ToString().AsSpan();
        var separatorIndex = name.IndexOf('.');
        if (separatorIndex == -1)
            throw new InvalidOperationException("Unable to parse property expression. Lacks member index expression");

        return name.Slice(separatorIndex + 1).ToString();
    }
}

public class FixturePropertyBuilder<TClass, TProperty> : IFixturePropertyBuilder<TClass, TProperty>
{
    internal List<IScenario<TClass, TProperty>> Scenarios { get; } = new();
    internal Expression<Func<TClass, TProperty>> PropertyExpression { get; }
    internal string Name { get; }

    public FixturePropertyBuilder(Expression<Func<TClass, TProperty>> propertyExpression, string? name = null)
    {
        PropertyExpression = propertyExpression;
        Name = GetPropertyName(propertyExpression, name);
    }

    public IFixturePropertyBuilder<TClass, TProperty> Valid(TProperty value, string? name = null)
    {
        AddScenario(value, name, FixtureState.Valid);
        
        return this;
    }

    public IFixturePropertyBuilder<TClass, TProperty> Invalid(TProperty value, string? name = null)
    {
        AddScenario(value, name, FixtureState.Invalid);

        return this;
    }

    private void AddScenario(TProperty value, string? name, FixtureState state)
    {
        name ??= GetPropertyName(value, name);
        var scenario = new GenericScenario<TClass, TProperty>(value, state, name);

        Scenarios.Add(scenario);
    }

    private static string GetPropertyName(object? value, string? name)
    {
        if (name is not null) return name;
        if (value is Expression<Func<TClass, TProperty>> propertyExpression) 
            return PropertyExpressionService.GetName(propertyExpression);

        return value?.ToString() ?? "null";
    }
}