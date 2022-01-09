using System;
using DomainFixture.TestGenerator.Framework;
using DomainFixture.TestGenerator.Framework.Strategies;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public class FrameworkStrategyFactoryTests
{
    [TestCase(TestingFramework.NUnit, typeof(NUnitStrategy))]
    [TestCase(TestingFramework.XUnit, typeof(XUnitStrategy))]
    [TestCase(TestingFramework.MSTest, typeof(MSTestStrategy))]
    public void CreateStrategy_ShouldReturnStrategy_WhenGivenCorrespondingTestingFramework(
        TestingFramework testingFramework, Type strategy)
    {
        // Act
        var result = FrameworkStrategyFactory.CreateStrategy(testingFramework);
        
        // Assert
        result.Should().BeOfType(strategy);
    }
}