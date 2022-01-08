using System.Collections.Generic;

namespace DomainFixture.FixtureConfigurations;

public interface IFixtureConfiguration
{
}

public interface IFixtureConfiguration<TClass> : IFixtureConfiguration
{
    public void Configure(IFixtureConfigurationBuilder<TClass> builder);
}