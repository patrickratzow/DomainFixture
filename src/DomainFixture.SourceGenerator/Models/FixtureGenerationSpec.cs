using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class FixtureGenerationSpec
{
    public string ConfigurationName { get; }
    public string NamespaceName { get; }
    public string RecipeName { get; }
    public string SubjectTypeName { get; }
    public string SubjectTypeShortName { get; }
    public string BaselineFactoryExpression { get; }
    public string? ValidatorFactoryExpression { get; }
    public string? ValidationRulesTypeKey { get; }
    public ImmutableArray<SubjectPropertySpec> SubjectProperties { get; }
    public Location? Location { get; }

    public FixtureGenerationSpec(
        string configurationName,
        string namespaceName,
        string recipeName,
        string subjectTypeName,
        string subjectTypeShortName,
        string baselineFactoryExpression,
        string? validatorFactoryExpression,
        string? validationRulesTypeKey,
        ImmutableArray<SubjectPropertySpec> subjectProperties,
        Location? location)
    {
        ConfigurationName = configurationName;
        NamespaceName = namespaceName;
        RecipeName = recipeName;
        SubjectTypeName = subjectTypeName;
        SubjectTypeShortName = subjectTypeShortName;
        BaselineFactoryExpression = baselineFactoryExpression;
        ValidatorFactoryExpression = validatorFactoryExpression;
        ValidationRulesTypeKey = validationRulesTypeKey;
        SubjectProperties = subjectProperties;
        Location = location;
    }
}
