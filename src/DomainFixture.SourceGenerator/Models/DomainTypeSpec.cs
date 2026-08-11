using System.Collections.Immutable;
using DomainFixture.Contracts;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class DomainTypeSpec
{
    public string SubjectTypeName { get; }
    public ImmutableArray<DomainRecipeSpec> Recipes { get; }
    public ImmutableArray<SubjectPropertySpec> Properties { get; }
    public ImmutableArray<DomainFact<DomainConstraintContract>> Constraints { get; }
    public ImmutableArray<DomainFact<DomainOperationContract>> ConstructionOperations { get; }
    public ImmutableArray<DomainFact<DomainOperationContract>> Operations { get; }
    public ImmutableArray<DomainFact<DomainOperationOutcomeContract>> Outcomes { get; }
    public ImmutableArray<DomainFact<DomainScenarioContract>> Scenarios { get; }

    public DomainTypeSpec(
        string subjectTypeName,
        ImmutableArray<DomainRecipeSpec> recipes,
        ImmutableArray<SubjectPropertySpec> properties,
        ImmutableArray<DomainFact<DomainConstraintContract>> constraints,
        ImmutableArray<DomainFact<DomainOperationContract>> constructionOperations,
        ImmutableArray<DomainFact<DomainOperationOutcomeContract>> outcomes,
        ImmutableArray<DomainFact<DomainScenarioContract>> scenarios,
        ImmutableArray<DomainFact<DomainOperationContract>> operations = default)
    {
        SubjectTypeName = subjectTypeName;
        Recipes = recipes;
        Properties = properties;
        Constraints = constraints;
        ConstructionOperations = constructionOperations;
        Operations = operations.IsDefault ? constructionOperations : operations;
        Outcomes = outcomes;
        Scenarios = scenarios;
    }
}
