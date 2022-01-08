using System;
using System.Collections.Generic;
using System.Reflection;

namespace DomainFixture.FixtureConfigurations;

public interface IFixtureConfigurationScanner
{
    public IEnumerable<Type> Configurations { get; }
    public void AddAssembly(Assembly assembly);
    public void AddAssemblies(params Assembly[] assemblies);
}