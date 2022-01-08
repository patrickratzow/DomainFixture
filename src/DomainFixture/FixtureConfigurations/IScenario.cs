using System;
using System.Linq.Expressions;

namespace DomainFixture.FixtureConfigurations;

public interface IScenario<TClass, TProperty>
{
    public Expression<Func<TClass, TProperty>> Expression { get; }
    public string Name { get; }
    public FixtureState State { get; }
}