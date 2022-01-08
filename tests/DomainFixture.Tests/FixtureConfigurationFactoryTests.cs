using System;
using DomainFixture.FixtureConfigurations;
using DomainFixture.Tests.FixtureConfigurations;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.Tests;

[TestFixture]
public class FixtureConfigurationFactoryTests
{
    private record Fixture;
    
    [Test]
    public void Factory_ShouldNotThrow_WhenGivenATypeThatImplementsIFixtureConfiguration()
    {
        // Arrange
        var type = typeof(UserConfiguration);

        // Act
        var result = () => FixtureConfigurationFactory.Create(type);

        // Assert
        result.Should().NotThrow();
    }
    
        
    [Test]
    public void Factory_ShouldThrow_WhenGivenNull()
    {
        // Act
        var result = () => FixtureConfigurationFactory.Create(null!);

        // Assert
        result.Should().Throw<ArgumentNullException>();
    }
    
    [Test]
    public void Factory_ShouldThrow_WhenGivenATypeThatDoesNotImplementIFixtureConfiguration()
    {
        // Arrange
        var type = typeof(Fixture);

        // Act
        var result = () => FixtureConfigurationFactory.Create(type);

        // Assert
        result.Should().Throw<InvalidCastException>();
    }
    
    [Test]
    public void Factory_ShouldCreateInstance_WhenGivenATypeThatImplementsIFixtureConfiguration()
    {
        // Arrange
        var type = typeof(UserConfiguration);

        // Act
        var result = FixtureConfigurationFactory.Create(type);

        // Assert
        result.Should().BeAssignableTo<IFixtureConfiguration>()
            .And.BeOfType<UserConfiguration>();
    }
}