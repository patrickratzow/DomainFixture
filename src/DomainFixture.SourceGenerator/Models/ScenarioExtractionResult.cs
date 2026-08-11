using DomainFixture.Contracts;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class ScenarioExtractionResult
{
    public DiscoveredDomainScenario? Scenario { get; }
    public Diagnostic? Diagnostic { get; }

    private ScenarioExtractionResult(DiscoveredDomainScenario? scenario, Diagnostic? diagnostic)
    {
        Scenario = scenario;
        Diagnostic = diagnostic;
    }

    public static ScenarioExtractionResult Success(
        DomainScenarioContract contract,
        Location? location = null) =>
        new(new DiscoveredDomainScenario(contract, location), diagnostic: null);

    public static ScenarioExtractionResult Failure(Diagnostic diagnostic) =>
        new(scenario: null, diagnostic);
}
