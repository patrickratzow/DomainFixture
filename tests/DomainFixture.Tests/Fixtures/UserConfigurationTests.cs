using System.Linq;
using DomainFixture.FixtureConfigurations;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.Tests.Fixtures;

[TestFixture]
public class UserConfigurationTests
{
    private FixtureConfiguration _config = null!;
    private FixtureConfigurationBuilder<Fixture> _configBuilder = null!;
    private record Fixture(int Foo1, int Foo2, int Foo3, int Foo4, int Foo5);
    private class FixtureConfiguration : IFixtureConfiguration<Fixture>
    {
        public void Configure(IFixtureConfigurationBuilder<Fixture> builder)
        {
            builder.Property(x => x.Foo1)
                .Valid(1)
                .Invalid(0);
            
            builder.Property(x => x.Foo2)
                .Valid(1)
                .Invalid(0);
            
            builder.Property(x => x.Foo3)
                .Valid(1)
                .Invalid(0);
            
            builder.Property(x => x.Foo4)
                .Valid(1)
                .Invalid(0);
            
            builder.Property(x => x.Foo5)
                .Valid(1)
                .Invalid(0);
        }
    }
    
    [SetUp]
    public void SetUp()
    {
        _config = new();
        _configBuilder = new();
    }
    
    [Test]
    public void PropertyBuilders_ShouldHaveCountOf5_WhenGivenConfigurationWith5Properties()
    {
        // Act
        _config.Configure(_configBuilder);

        // Assert
        _configBuilder.PropertyBuilders.Should().HaveCount(5);
    }

    [Test]
    public void PropertyBuilders_ShouldContain10GenericScenarios_WhenGivenFixtureConfiguration()
    {
        // Act
        _config.Configure(_configBuilder);

        // Assert
        var scenarios = _configBuilder.PropertyBuilders
            .Select(x => (IFixturePropertyBuilder<Fixture, int>)x)
            .SelectMany(x => x.Scenarios)
            .ToList();
        scenarios.Should().HaveCount(10);
        
        var valid = scenarios.Where(s => s.State is FixtureState.Valid).ToList();
        var invalid = scenarios.Where(s => s.State is FixtureState.Invalid).ToList();
        valid.Should().HaveCount(5);
        invalid.Should().HaveCount(5);
        valid.Should().NotIntersectWith(invalid);
    }
}