using DomainFixture.TestGenerator.Framework.Strategies;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public class FrameworkStrategyTests
{
    [Test]
    public void ToString_ShouldWriteTest_WhenUsingNUnitStrategy()
    {
        // Arrange
        var strategy = new NUnitStrategy();
        
        // Act
        var (testAttributes, classAttribute) = strategy.CreateAttributes();

        // Assert
        classAttribute.Should().NotBeNull()
            .And.Subject.ToString().Should().Be("[TestFixture]");
        testAttributes.Should().HaveCount(1)
            .And.SatisfyRespectively(
                x =>
                {
                    x.ToString().Should().Be("[Test]");
                    x.Name.Should().Be("Test");
                    x.Parameters.Should().BeNull();
                }
            );
    }
    
    [Test]
    public void ToString_ShouldWriteTest_WhenUsingXUnitStrategy()
    {
        // Arrange
        var strategy = new XUnitStrategy();
        
        // Act
        var (testAttributes, classAttribute) = strategy.CreateAttributes();

        // Assert
        classAttribute.Should().BeNull();
        testAttributes.Should().HaveCount(1)
            .And.SatisfyRespectively(
                x =>
                {
                    x.ToString().Should().Be("[Fact]");
                    x.Name.Should().Be("Fact");
                    x.Parameters.Should().BeNull();
                }
            );
    }
    
    [Test]
    public void ToString_ShouldWriteTest_WhenUsingMSTestStrategy()
    {
        // Arrange
        var strategy = new MSTestStrategy();
        
        // Act
        var (testAttributes, classAttribute) = strategy.CreateAttributes();

        // Assert
        classAttribute.Should().NotBeNull()
            .And.Subject.ToString().Should().Be("[TestClass]");
        testAttributes.Should().HaveCount(1)
            .And.SatisfyRespectively(
                x =>
                {
                    x.ToString().Should().Be("[TestMethod]");
                    x.Name.Should().Be("TestMethod");
                    x.Parameters.Should().BeNull();
                }
            );
    }
}