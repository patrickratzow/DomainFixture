using System.Collections.Generic;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Generation;

internal static class DomainTransitionScenarioKinds
{
    public const string StateTransition = "domainfixture.scenario.state-transition";
    public const string RejectedTransition = "domainfixture.scenario.rejected-transition";
    public const string StateExpectation = "domainfixture.scenario.state-expectation";
}

internal static class DomainTransitionScenarioContractFactory
{
    public const string ScenarioNameParameter = "ScenarioName";

    public static DomainScenarioContract CreateState(
        FixtureGenerationSpec configuration,
        DomainStateExpectationSpec state)
    {
        return new DomainScenarioContract(
            DomainTransitionScenarioKinds.StateExpectation,
            $"global::{configuration.NamespaceName}.{configuration.ConfigurationName}",
            configuration.SubjectTypeName,
            new Dictionary<string, string>
            {
                [ScenarioNameParameter] = state.Name
            });
    }

    public static DomainScenarioContract Create(
        FixtureGenerationSpec configuration,
        DomainTransitionSpec transition)
    {
        return new DomainScenarioContract(
            transition.IsRejection
                ? DomainTransitionScenarioKinds.RejectedTransition
                : DomainTransitionScenarioKinds.StateTransition,
            $"global::{configuration.NamespaceName}.{configuration.ConfigurationName}",
            configuration.SubjectTypeName,
            new Dictionary<string, string>
            {
                [ScenarioNameParameter] = transition.Name
            });
    }
}
