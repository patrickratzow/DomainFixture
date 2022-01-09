using System;
using System.Collections.Generic;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
using System.Linq.Expressions;

namespace DomainFixture.FixtureConfigurations;

public class InvalidPropertyExpressionException : ArgumentException
{
    public InvalidPropertyExpressionException(string message, string parameterName) : base(message, parameterName)
    {
    }    
}

internal static class PropertyExpressionService 
{
    /// <summary>
    /// Parses the property expression to extract the name of what it points to
    /// </summary>
    /// <param name="propertyExpression">The property expression</param>
    /// <typeparam name="TClass">The fixture class</typeparam>
    /// <typeparam name="TProperty">The property</typeparam>
    /// <returns>The name of the property the expression points to</returns>
    /// <exception cref="InvalidPropertyExpressionException">The expression is invalid</exception>
    internal static string GetPropertyName<TClass, TProperty>(
        Expression<Func<TClass, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body.NodeType is not ExpressionType.MemberAccess)
            throw new InvalidPropertyExpressionException("Is not a member access expression", 
                nameof(propertyExpression));

        var name = propertyExpression.Body.ToString().AsSpan();
        var separatorIndex = name.IndexOf('.');
        if (separatorIndex == -1)
            throw new InvalidPropertyExpressionException(
                "Unable to parse property expression. Lacks dot expression", nameof(propertyExpression));
        // Two dots = nested member access
        if (name.LastIndexOf('.') != separatorIndex)
            throw new InvalidPropertyExpressionException("Invalid property expression. Only two dots are allowed",
                nameof(propertyExpression));
        
        return name.Slice(separatorIndex + 1).ToString();
    }
}

public class FixturePropertyBuilder<TClass, TProperty> : IFixturePropertyBuilder<TClass, TProperty>
{
    List<IScenario<TClass, TProperty>> IFixturePropertyBuilder<TClass, TProperty>.Scenarios { get; } = new();
    Expression<Func<TClass, TProperty>> IFixturePropertyBuilder<TClass, TProperty>.PropertyExpression { get; set; } 
        = null!;
    string IFixturePropertyBuilder<TClass, TProperty>.Name { get; set; } = null!;

    public FixturePropertyBuilder(Expression<Func<TClass, TProperty>> propertyExpression, string? name = null)
    {
        ((IFixturePropertyBuilder<TClass, TProperty>)this).PropertyExpression = propertyExpression;
        ((IFixturePropertyBuilder<TClass, TProperty>)this).Name = GetPropertyName(propertyExpression, name);
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

        ((IFixturePropertyBuilder<TClass, TProperty>)this).Scenarios.Add(scenario);
    }

    private static string GetPropertyName(object? value, string? name)
    {
        if (name is not null) return name;
        if (value is Expression<Func<TClass, TProperty>> propertyExpression) 
            return PropertyExpressionService.GetPropertyName(propertyExpression);

        return value?.ToString() ?? "null";
    }
    
}