using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DomainFixture.FixtureConfigurations;

public class FixtureConfigurationBuilder<TClass> : IFixtureConfigurationBuilder<TClass>
{
    internal List<IFixturePropertyBuilder<TClass>> PropertyBuilders = new();
    
    public IFixturePropertyBuilder<TClass, TProperty> Property<TProperty>(
        Expression<Func<TClass, TProperty>> propertyExpression)
    {
        var builder = new FixturePropertyBuilder<TClass, TProperty>(propertyExpression);
        
        PropertyBuilders.Add(builder);

        return builder;
    }
}