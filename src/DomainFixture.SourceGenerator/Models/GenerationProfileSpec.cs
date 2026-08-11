using System.Collections.Immutable;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class GenerationProfileSpec
{
    public static GenerationProfileSpec Default { get; } = new(
        useNullability: false,
        usePropertyNames: false,
        useImmutableObjects: false,
        useEntityIdentity: false,
        useFluentValidation: false,
        FixtureActivationKind.Factories,
        serviceProviderFactoryType: null,
        ImmutableArray<PropertyMutationSpec>.Empty,
        ImmutableArray<OperationRejectionSpec>.Empty,
        ImmutableArray<OperationResultSpec>.Empty,
        ImmutableArray<ConfiguredValueSpec>.Empty);

    public bool UseNullability { get; }
    public bool UsePropertyNames { get; }
    public bool UseImmutableObjects { get; }
    public bool UseEntityIdentity { get; }
    public bool UseFluentValidation { get; }
    public FixtureActivationKind ActivationKind { get; }
    public string? ServiceProviderFactoryType { get; }
    public ImmutableArray<PropertyMutationSpec> PropertyMutations { get; }
    public ImmutableArray<OperationRejectionSpec> OperationRejections { get; }
    public ImmutableArray<OperationResultSpec> OperationResults { get; }
    public ImmutableArray<ConfiguredValueSpec> ConfiguredValues { get; }

    public GenerationProfileSpec(
        bool useNullability,
        bool usePropertyNames,
        bool useImmutableObjects,
        bool useEntityIdentity,
        bool useFluentValidation,
        FixtureActivationKind activationKind,
        string? serviceProviderFactoryType,
        ImmutableArray<PropertyMutationSpec> propertyMutations,
        ImmutableArray<OperationRejectionSpec> operationRejections,
        ImmutableArray<OperationResultSpec> operationResults = default,
        ImmutableArray<ConfiguredValueSpec> configuredValues = default)
    {
        UseNullability = useNullability;
        UsePropertyNames = usePropertyNames;
        UseImmutableObjects = useImmutableObjects;
        UseEntityIdentity = useEntityIdentity;
        UseFluentValidation = useFluentValidation;
        ActivationKind = activationKind;
        ServiceProviderFactoryType = serviceProviderFactoryType;
        PropertyMutations = propertyMutations;
        OperationRejections = operationRejections;
        OperationResults = operationResults.IsDefault
            ? ImmutableArray<OperationResultSpec>.Empty
            : operationResults;
        ConfiguredValues = configuredValues.IsDefault
            ? ImmutableArray<ConfiguredValueSpec>.Empty
            : configuredValues;
    }

    public OperationRejectionSpec? ResolveOperationRejection(string subjectTypeKey)
    {
        foreach (var rejection in OperationRejections)
        {
            if (rejection.SubjectTypeKey == subjectTypeKey)
                return rejection;
        }

        foreach (var rejection in OperationRejections)
        {
            if (rejection.SubjectTypeKey is null)
                return rejection;
        }

        return null;
    }

    public OperationResultSpec? ResolveOperationResult(
        string subjectTypeName,
        string resultTypeName)
    {
        foreach (var result in OperationResults)
        {
            if (result.SubjectTypeName == subjectTypeName &&
                result.ResultTypeName == resultTypeName)
                return result;
        }

        return null;
    }

    public ConfiguredValueSpec? ResolveConfiguredValue(string typeName)
    {
        foreach (var value in ConfiguredValues)
        {
            if (value.TypeName == typeName)
                return value;
        }

        return null;
    }
}
