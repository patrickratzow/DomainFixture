using System;
using System.Collections.Generic;
using System.Linq;
using DomainFixture.Factories.Generic.Properties;
using DomainFixture.Factories.Generic.Properties.Strategies;

namespace DomainFixture.Factories.Generic;

public class ObjectFactory<T> : IObjectFactory<T> where T : notnull
{
    public T Create(IObjectState state)
    {
        var instance = CreateInstance();
        var properties = state.Properties;

        ApplyProperties(instance, properties);

        return instance;
    }

    private void ApplyProperties(T instance, IEnumerable<IObjectProperty> properties)
    {
        foreach (var objectProperty in properties)
        {
            var strategy = GetApplyPropertyStrategy(objectProperty);
            strategy.Apply(instance, objectProperty);
        }
    }

    private static IApplyPropertyStrategy GetApplyPropertyStrategy(IObjectProperty objectProperty) =>
        objectProperty switch
        {
            _ => new GenericApplyPropertyStrategy()
        };

    private static T CreateInstance()
    {
        return Activator.CreateInstance<T>();
    }
}