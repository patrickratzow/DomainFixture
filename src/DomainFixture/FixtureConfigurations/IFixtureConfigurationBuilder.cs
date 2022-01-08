using System;
using System.Linq.Expressions;

namespace DomainFixture.FixtureConfigurations;

public interface IFixtureConfigurationBuilder<TClass>
{
    public IFixturePropertyBuilder<TClass, TProperty> Property<TProperty>(
        Expression<Func<TClass, TProperty>> propertyExpression);
}