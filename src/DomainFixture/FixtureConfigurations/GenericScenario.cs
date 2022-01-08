using System;
using System.Linq.Expressions;

namespace DomainFixture.FixtureConfigurations;

public class GenericScenario<TClass, TProperty> : IScenario<TClass, TProperty>
{
    public Expression<Func<TClass, TProperty>> Expression { get; }
    public string Name { get; }
    public FixtureState State { get; }

    public GenericScenario(TProperty value, FixtureState state, string name) : this(x => value, state, name)
    {
    }

    public GenericScenario(Expression<Func<TClass, TProperty>> expression, FixtureState state, string name)
    {
        Expression = expression;
        State = state;
        Name = name;
    }
}