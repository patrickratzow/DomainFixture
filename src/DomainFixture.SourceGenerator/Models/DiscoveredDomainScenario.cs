using DomainFixture.Contracts;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class DiscoveredDomainScenario
{
    public DomainScenarioContract Contract { get; }
    public Location? Location { get; }

    public DiscoveredDomainScenario(DomainScenarioContract contract, Location? location)
    {
        Contract = contract;
        Location = location;
    }
}
