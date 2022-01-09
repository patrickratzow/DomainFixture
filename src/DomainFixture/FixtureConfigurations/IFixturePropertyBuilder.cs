using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DomainFixture.FixtureConfigurations;

public interface IFixturePropertyBuilder<TClass>
{
}

public interface IFixturePropertyBuilder<TClass, TProperty> : IFixturePropertyBuilder<TClass>
{
    internal List<IScenario<TClass, TProperty>> Scenarios { get; }
    internal Expression<Func<TClass, TProperty>> PropertyExpression { get; set; }
    internal string Name { get; set; }

    public IFixturePropertyBuilder<TClass, TProperty> Valid(TProperty value, string? name = null);
    public IFixturePropertyBuilder<TClass, TProperty> Invalid(TProperty value, string? name = null);
}