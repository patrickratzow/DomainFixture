using System;

namespace DomainFixture.TestGenerator.Model.Properties;

public sealed class PropertyDescriptor
{
    public string Name { get; }

    public PropertyDescriptor(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A property descriptor must have a name.", nameof(name));

        Name = name;
    }
}
