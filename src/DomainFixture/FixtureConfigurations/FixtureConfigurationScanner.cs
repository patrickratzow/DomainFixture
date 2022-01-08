using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DomainFixture.Extensions;

namespace DomainFixture.FixtureConfigurations;

public class FixtureConfigurationScanner : IFixtureConfigurationScanner
{
    private readonly HashSet<Type> _configurations = new();
    public IEnumerable<Type> Configurations => _configurations;

    public void AddAssembly(Assembly assembly) => Scan(assembly);
    public void AddAssemblies(params Assembly[] assemblies) => Scan(assemblies);

    private void Scan(params Assembly[] assemblies)
    {
        var configurations = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i => i.IsDerivedOfGenericType(typeof(IFixtureConfiguration<>))));

        foreach (var configuration in configurations)
        {
            _configurations.Add(configuration);
        }
    }
}