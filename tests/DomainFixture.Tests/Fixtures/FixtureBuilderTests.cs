using DomainFixture.Tests.Domain.Entities;
using NUnit.Framework;
using FluentAssertions;

namespace DomainFixture.Tests.Fixtures;

[TestFixture]
public class FixtureBuilderTests
{
    [Test]
    public void Create_CreatesInstanceOfFixture()
    {
        // Arrange
        var action = () => DomainFixture.Valid<User>().Create();

        // Act
        var result = action.Invoke();
        
        // Assert
        result.Should().NotBeNull();
    }

    [Test] 
    public void CreateMany_CreatesThreeInstancesOfFixture_WhenNoCountIsNotProvided()
    {
        // Arrange
        var action = () => DomainFixture.Valid<User>().CreateMany();

        // Act
        var result = action.Invoke();
        
        // Assert
        result.Should().HaveCount(3);
    }
    
    [Test] 
    public void CreateMany_CreatesCountAmountOfInstancesOfFixture_WhenNoCountIsProvided()
    {
        // Arrange
        const int count = 10;
        var action = () => DomainFixture.Valid<User>().CreateMany(count);

        // Act
        var result = action.Invoke();
        
        // Assert
        result.Should().HaveCount(count);
    }
}