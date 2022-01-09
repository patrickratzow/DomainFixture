using System;
using System.Linq.Expressions;
using DomainFixture.FixtureConfigurations;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.Tests;

[TestFixture]
public class PropertyExpressionServiceTests
{
    private record Fixture(string Name)
    {
        // Just used for a single test, so this being this hardcoded is fine
        public string this[string name] => (string)GetType().GetProperty(name)!.GetValue(this)!;
    }

    [Test]
    public void GetPropertyName_ShouldReturnName_WhenGivenSimplePropertyExpression()
    {
        // Arrange
        Expression<Func<Fixture, string>> expression = x => x.Name;

        // Act
        var result = PropertyExpressionService.GetPropertyName(expression);

        // Assert
        result.Should().Be(nameof(Fixture.Name));
    }
    
    [Test]
    public void GetPropertyName_ShouldThrowInvalidPropertyExpressionException_WhenGivenAnExpressionWithTwoDots()
    {
        // Arrange
        Expression<Func<Fixture, int>> expression = x => x.Name.Length;

        // Act
        var result = () => PropertyExpressionService.GetPropertyName(expression);

        // Assert
        result.Should().ThrowExactly<InvalidPropertyExpressionException>();
    }
    
    [Test]
    public void GetPropertyName_ShouldThrowInvalidPropertyExpressionException_WhenGivenAnExpressionWithAnIndexer()
    {
        // Arrange
        Expression<Func<Fixture, string>> expression = x => x["Name"];

        // Act
        var result = () => PropertyExpressionService.GetPropertyName(expression);

        // Assert
        result.Should().ThrowExactly<InvalidPropertyExpressionException>();
    }
    
    [Test]
    public void GetPropertyName_ShouldThrowInvalidPropertyExpressionException_WhenPointingToItself()
    {
        // Arrange
        Expression<Func<Fixture, Fixture>> expression = x => x;

        // Act
        var result = () => PropertyExpressionService.GetPropertyName(expression);

        // Assert
        result.Should().ThrowExactly<InvalidPropertyExpressionException>();
    }
}