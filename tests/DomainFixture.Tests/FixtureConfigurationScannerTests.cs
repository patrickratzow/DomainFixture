using DomainFixture.FixtureConfigurations;
using DomainFixture.Tests.FixtureConfigurations;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.Tests;

[TestFixture]
public class FixtureConfigurationScannerTests
{
    [Test]
    public void Scan_ShouldFindConfigurations_WhenGivenAssembly()
    {
        // Arrange
        var assembly = typeof(FixtureConfigurationScannerTests).Assembly;
        var scanner = new FixtureConfigurationScanner();
        scanner.AddAssembly(assembly);

        // Act
        var configurations = scanner.Configurations;

        // Assert
        configurations.Should().HaveCount(1)
            .And.Satisfy(
                first => first.Name.Equals(nameof(UserConfiguration))
            );
    }
}