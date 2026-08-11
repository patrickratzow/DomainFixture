using System.Collections.Generic;
using System.Linq;
using DomainFixture.Pipeline;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Discovery;

internal static class PropertyMutationStrategyPipeline
{
    private static readonly ProviderPipeline<PropertyMutationRequest, PropertyMutationResolution>
        Pipeline = new(
            new IPipelineProvider<PropertyMutationRequest, PropertyMutationResolution>[]
            {
                new ExplicitPropertyMutationProvider(),
                new DirectPropertyMutationProvider(),
                new RecordPropertyMutationProvider(),
                new ConstructorPropertyMutationProvider(),
                new DerivedTypePropertyMutationProvider()
            },
            ProviderPipelineMode.FirstHandled);

    public static PropertyMutationResolution Resolve(
        FixtureGenerationSpec configuration,
        GenerationProfileSpec profile,
        SubjectPropertySpec property,
        IReadOnlyDictionary<string, PropertyMutationSpec> explicitMutations,
        string reconstructionClassName)
    {
        var request = new PropertyMutationRequest(
            configuration,
            profile,
            property,
            explicitMutations,
            reconstructionClassName);
        var resolution = Pipeline.Resolve(request);
        if (resolution.Kind == ProviderResolutionKind.Handled)
            return resolution.Output!;

        return PropertyMutationResolution.Failure(
            property,
            profile.UseImmutableObjects
                ? "the type has no usable record 'with', state-preserving constructor, derived-type, or explicit mutation strategy"
                : "immutable-object conventions are not enabled");
    }
}

internal sealed class PropertyMutationRequest
{
    public FixtureGenerationSpec Configuration { get; }
    public GenerationProfileSpec Profile { get; }
    public SubjectPropertySpec Property { get; }
    public IReadOnlyDictionary<string, PropertyMutationSpec> ExplicitMutations { get; }
    public string ReconstructionClassName { get; }

    public PropertyMutationRequest(
        FixtureGenerationSpec configuration,
        GenerationProfileSpec profile,
        SubjectPropertySpec property,
        IReadOnlyDictionary<string, PropertyMutationSpec> explicitMutations,
        string reconstructionClassName)
    {
        Configuration = configuration;
        Profile = profile;
        Property = property;
        ExplicitMutations = explicitMutations;
        ReconstructionClassName = reconstructionClassName;
    }

    public PropertyMutationSpec CreateGeneratedMutation() => new(
        Configuration.SubjectTypeName,
        Property.Name,
        $"global::{Configuration.NamespaceName}.{ReconstructionClassName}.With{Property.Name}");
}

internal sealed class ExplicitPropertyMutationProvider :
    IPipelineProvider<PropertyMutationRequest, PropertyMutationResolution>
{
    public string Id => "domainfixture.mutations.explicit";

    public ProviderDecision<PropertyMutationResolution> Evaluate(PropertyMutationRequest request)
    {
        return request.ExplicitMutations.TryGetValue(request.Property.Name, out var mutation)
            ? ProviderDecision<PropertyMutationResolution>.Handled(
                PropertyMutationResolution.Success(
                    request.Property,
                    PropertyMutationStrategyKind.Explicit,
                    mutation))
            : ProviderDecision<PropertyMutationResolution>.NotHandled();
    }
}

internal sealed class DirectPropertyMutationProvider :
    IPipelineProvider<PropertyMutationRequest, PropertyMutationResolution>
{
    public string Id => "domainfixture.mutations.direct";

    public ProviderDecision<PropertyMutationResolution> Evaluate(PropertyMutationRequest request) =>
        request.Property.CanBeAssigned
            ? ProviderDecision<PropertyMutationResolution>.Handled(
                PropertyMutationResolution.Success(
                    request.Property,
                    PropertyMutationStrategyKind.DirectAssignment))
            : ProviderDecision<PropertyMutationResolution>.NotHandled();
}

internal sealed class RecordPropertyMutationProvider :
    IPipelineProvider<PropertyMutationRequest, PropertyMutationResolution>
{
    public string Id => "domainfixture.mutations.record-with";

    public ProviderDecision<PropertyMutationResolution> Evaluate(PropertyMutationRequest request) =>
        request.Profile.UseImmutableObjects &&
        request.Configuration.IsRecord &&
        request.Property.CanSetInObjectInitializer
            ? ProviderDecision<PropertyMutationResolution>.Handled(
                PropertyMutationResolution.Success(
                    request.Property,
                    PropertyMutationStrategyKind.RecordWith,
                    request.CreateGeneratedMutation()))
            : ProviderDecision<PropertyMutationResolution>.NotHandled();
}

internal sealed class ConstructorPropertyMutationProvider :
    IPipelineProvider<PropertyMutationRequest, PropertyMutationResolution>
{
    public string Id => "domainfixture.mutations.constructor";

    public ProviderDecision<PropertyMutationResolution> Evaluate(PropertyMutationRequest request)
    {
        if (!request.Profile.UseImmutableObjects)
            return ProviderDecision<PropertyMutationResolution>.NotHandled();

        var constructor = request.Configuration.ReconstructionConstructors.FirstOrDefault(candidate =>
            candidate.Parameters.Any(parameter => parameter.PropertyName == request.Property.Name));
        return constructor is null
            ? ProviderDecision<PropertyMutationResolution>.NotHandled()
            : ProviderDecision<PropertyMutationResolution>.Handled(
                PropertyMutationResolution.Success(
                    request.Property,
                    PropertyMutationStrategyKind.Constructor,
                    request.CreateGeneratedMutation(),
                    constructor));
    }
}

internal sealed class DerivedTypePropertyMutationProvider :
    IPipelineProvider<PropertyMutationRequest, PropertyMutationResolution>
{
    public string Id => "domainfixture.mutations.derived-type";

    public ProviderDecision<PropertyMutationResolution> Evaluate(PropertyMutationRequest request) =>
        request.Profile.UseImmutableObjects &&
        request.Configuration.CanUseDerivedReconstruction &&
        request.Property.CanSetFromDerivedType
            ? ProviderDecision<PropertyMutationResolution>.Handled(
                PropertyMutationResolution.Success(
                    request.Property,
                    PropertyMutationStrategyKind.DerivedType,
                    request.CreateGeneratedMutation()))
            : ProviderDecision<PropertyMutationResolution>.NotHandled();
}
