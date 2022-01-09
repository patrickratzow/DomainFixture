using System;
using DomainFixture.TestGenerator.Framework.Strategies;

namespace DomainFixture.TestGenerator.Framework;

public static class FrameworkStrategyFactory
{
    public static IFrameworkStrategy CreateStrategy(TestingFramework testingFramework)
        => testingFramework switch
        {
            TestingFramework.NUnit => new NUnitStrategy(),
            TestingFramework.XUnit => new XUnitStrategy(),
            TestingFramework.MSTest => new MSTestStrategy(),
            _ => throw new ArgumentOutOfRangeException(nameof(testingFramework), testingFramework, null)
        };
}