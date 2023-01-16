using System.Diagnostics;

namespace DomainFixture.Factories.Generic.Properties.Strategies;

public class GenericApplyPropertyStrategy : IApplyPropertyStrategy
{
    public void Apply<T>(T instance, IObjectProperty objectProperty) where T : notnull
    {
        var key = objectProperty.Key.Value;
        var property = instance.GetType().GetProperty(key);
        if (property is null)
        {
            Debug.WriteLine($"{instance.GetType().Name} could not find any property named {key}");
            
            return;
        }

        var value = objectProperty.Value.Value;
        property.SetValue(instance, value);
    }
}