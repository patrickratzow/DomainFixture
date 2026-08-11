using System.Collections.Immutable;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class GenerationProfileSpec
{
    public static GenerationProfileSpec Default { get; } = new(
        useNullability: false,
        usePropertyNames: false,
        useImmutableObjects: false,
        useFluentValidation: false,
        FixtureActivationKind.Factories,
        serviceProviderFactoryType: null,
        ImmutableArray<PropertyMutationSpec>.Empty);

    public bool UseNullability { get; }
    public bool UsePropertyNames { get; }
    public bool UseImmutableObjects { get; }
    public bool UseFluentValidation { get; }
    public FixtureActivationKind ActivationKind { get; }
    public string? ServiceProviderFactoryType { get; }
    public ImmutableArray<PropertyMutationSpec> PropertyMutations { get; }

    public GenerationProfileSpec(
        bool useNullability,
        bool usePropertyNames,
        bool useImmutableObjects,
        bool useFluentValidation,
        FixtureActivationKind activationKind,
        string? serviceProviderFactoryType,
        ImmutableArray<PropertyMutationSpec> propertyMutations)
    {
        UseNullability = useNullability;
        UsePropertyNames = usePropertyNames;
        UseImmutableObjects = useImmutableObjects;
        UseFluentValidation = useFluentValidation;
        ActivationKind = activationKind;
        ServiceProviderFactoryType = serviceProviderFactoryType;
        PropertyMutations = propertyMutations;
    }
}
