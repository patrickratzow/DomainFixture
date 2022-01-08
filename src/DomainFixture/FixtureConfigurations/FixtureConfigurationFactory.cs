using System;

namespace DomainFixture.FixtureConfigurations;

/// <summary>
/// Factory to create <see cref="IFixtureConfiguration"/> from types
/// </summary>
public static class FixtureConfigurationFactory
{
    /// <summary>
    /// Creates an instance of <see cref="IFixtureConfiguration"/>
    /// </summary>
    /// <param name="type">The type</param>
    /// <returns>An instance of the type as a <see cref="IFixtureConfiguration"/></returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="type"/> is null</exception>
    /// <exception cref="InvalidCastException">If the <paramref name="type"/> did not implement <see cref="IFixtureConfiguration"/></exception>
    public static IFixtureConfiguration Create(Type type)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        if (Activator.CreateInstance(type) is not IFixtureConfiguration instance)
            throw new InvalidCastException($"Unable to cast {type.Name} to {nameof(IFixtureConfiguration)}");
        
        return instance;
    }
}