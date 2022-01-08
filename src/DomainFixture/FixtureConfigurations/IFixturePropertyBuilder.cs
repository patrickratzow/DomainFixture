using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace DomainFixture.FixtureConfigurations;

public interface IFixturePropertyBuilder<TClass>
{
}

public interface IFixturePropertyBuilder<TClass, TProperty> : IFixturePropertyBuilder<TClass>
{
    public IFixturePropertyBuilder<TClass, TProperty> Valid(TProperty value, 
        [CallerArgumentExpression("value")] string? name = null);
    public IFixturePropertyBuilder<TClass, TProperty> Invalid(TProperty value, 
        [CallerArgumentExpression("value")] string? name = null);
}

public class FixturePropertyBuilder<TClass, TProperty> : IFixturePropertyBuilder<TClass, TProperty>
{
    internal List<IScenario<TClass, TProperty>> Scenarios { get; } = new();
    internal Expression<Func<TClass, TProperty>> PropertyExpression { get; }
    internal string Name { get; }

    public FixturePropertyBuilder(Expression<Func<TClass, TProperty>> propertyExpression,
        [CallerArgumentExpression("propertyExpression")] string? name = null)
    {
        PropertyExpression = propertyExpression;
        Name = name!;
    }

    public IFixturePropertyBuilder<TClass, TProperty> Valid(TProperty value, 
        [CallerArgumentExpression("value")] string? name = null)
    {
        AddScenario(value, name!, FixtureState.Valid);
        
        return this;
    }

    public IFixturePropertyBuilder<TClass, TProperty> Invalid(TProperty value, 
        [CallerArgumentExpression("value")] string? name = null)
    {
        AddScenario(value, name!, FixtureState.Invalid);

        return this;
    }

    private void AddScenario(TProperty value, string name, FixtureState state)
    {
        var scenario = new GenericScenario<TClass, TProperty>(value, state, name);

        Scenarios.Add(scenario);
    }
}

public interface IScenario<TClass, TProperty>
{
    public string Name { get; }
    public FixtureState State { get; }
}

public class GenericScenario<TClass, TProperty> : IScenario<TClass, TProperty>
{
    private readonly Expression<Func<TClass, TProperty>> _expression;
    public string Name { get; }
    public FixtureState State { get; }

    public GenericScenario(TProperty value, FixtureState state, string name) : this(x => value, state, name)
    {
    }

    public GenericScenario(Expression<Func<TClass, TProperty>> expression, FixtureState state, string name)
    {
        _expression = expression;
        State = state;
        Name = name;
    }
}