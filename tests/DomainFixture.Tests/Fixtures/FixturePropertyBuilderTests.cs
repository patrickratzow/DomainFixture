using DomainFixture.FixtureConfigurations;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.Tests.Fixtures;

[TestFixture]
public class FixturePropertyBuilderTests
{
    private record Fixture(string Name);

    [Test]
    public void Constructor_ShouldSetNameToExpression_WhenNameIsNull()
    {
        // Act
        var propertyBuilder = new FixturePropertyBuilder<Fixture, string>(x => x.Name);

        // Assert
        propertyBuilder.Name.Should().Be("Name");
    }

    [Test]
    public void Constructor_ShouldSetName_WhenExpressionIsNotNull()
    {
        // Arrange
        var name = "Username";

        // Act
        var propertyBuilder = new FixturePropertyBuilder<Fixture, string>(x => x.Name, name);

        // Assert
        propertyBuilder.Name.Should().Be(name);
    }

    [Test]
    public void Valid_ShouldAddGenericScenario_WhenGivenAnExpression()
    {
        // Arrange
        var name = "Baksling";
        var propertyBuilder = new FixturePropertyBuilder<Fixture, string>(x => x.Name);
        
        // Act
        propertyBuilder.Valid(name, name);

        // Assert
        propertyBuilder.Scenarios.Should().HaveCount(1)
            .And.SatisfyRespectively(
                scenario =>
                {
                    scenario.Name.Should().Be(name);
                    scenario.State.Should().Be(FixtureState.Valid);
                }
            );
    }
    
    [Test]
    public void Invalid_ShouldAddGenericScenario_WhenGivenAnExpression()
    {
        // Arrange
        var name = "Baksling";
        var propertyBuilder = new FixturePropertyBuilder<Fixture, string>(x => x.Name);
        
        // Act
        propertyBuilder.Invalid(name, name);

        // Assert
        propertyBuilder.Scenarios.Should().HaveCount(1)
            .And.SatisfyRespectively(
                scenario =>
                {
                    scenario.Name.Should().Be(name);
                    scenario.State.Should().Be(FixtureState.Invalid);
                }
            );
    }
}