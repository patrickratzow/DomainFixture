using System.Collections.Immutable;
using DomainFixture.Contracts;
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
    public bool IsRecord { get; }
    public ImmutableArray<SubjectConstructorSpec> ReconstructionConstructors { get; }
    public bool CanUseDerivedReconstruction { get; }
    public bool HasValueEqualitySemantics { get; }
    public string? EquivalentCopyExpression { get; }
    public ImmutableArray<DomainOperationContract> ConstructionOperations { get; }
    public Location? Location { get; }
    public bool CanExposePublicFactory { get; }
    public ImmutableArray<DomainTransitionSpec> Transitions { get; }
    public string? IdentityMemberPath { get; }
    public bool UsesSynthesizedBaseline { get; }
    public ImmutableArray<DomainStateExpectationSpec> StateExpectations { get; }

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
        bool isRecord,
        ImmutableArray<SubjectConstructorSpec> reconstructionConstructors,
        bool canUseDerivedReconstruction,
        bool hasValueEqualitySemantics,
        string? equivalentCopyExpression,
        ImmutableArray<DomainOperationContract> constructionOperations,
        Location? location,
        bool canExposePublicFactory = false,
        ImmutableArray<DomainTransitionSpec> transitions = default,
        string? identityMemberPath = null,
        bool usesSynthesizedBaseline = false,
        ImmutableArray<DomainStateExpectationSpec> stateExpectations = default)
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
        IsRecord = isRecord;
        ReconstructionConstructors = reconstructionConstructors;
        CanUseDerivedReconstruction = canUseDerivedReconstruction;
        HasValueEqualitySemantics = hasValueEqualitySemantics;
        EquivalentCopyExpression = equivalentCopyExpression;
        ConstructionOperations = constructionOperations;
        Location = location;
        CanExposePublicFactory = canExposePublicFactory;
        Transitions = transitions.IsDefault
            ? ImmutableArray<DomainTransitionSpec>.Empty
            : transitions;
        IdentityMemberPath = identityMemberPath;
        UsesSynthesizedBaseline = usesSynthesizedBaseline;
        StateExpectations = stateExpectations.IsDefault
            ? ImmutableArray<DomainStateExpectationSpec>.Empty
            : stateExpectations;
    }

    public FixtureGenerationSpec WithBaselineFactoryExpression(string baselineFactoryExpression) =>
        new(
            ConfigurationName,
            NamespaceName,
            RecipeName,
            SubjectTypeName,
            SubjectTypeShortName,
            baselineFactoryExpression,
            ValidatorFactoryExpression,
            ValidationRulesTypeKey,
            SubjectProperties,
            IsRecord,
            ReconstructionConstructors,
            CanUseDerivedReconstruction,
            HasValueEqualitySemantics,
            EquivalentCopyExpression,
            ConstructionOperations,
            Location,
            CanExposePublicFactory,
            Transitions,
            IdentityMemberPath,
            UsesSynthesizedBaseline,
            StateExpectations);
}
