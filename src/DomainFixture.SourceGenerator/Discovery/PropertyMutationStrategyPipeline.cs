using System.Collections.Generic;
using System.Linq;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Discovery;

internal static class PropertyMutationStrategyPipeline
{
    public static PropertyMutationResolution Resolve(
        FixtureGenerationSpec configuration,
        GenerationProfileSpec profile,
        SubjectPropertySpec property,
        IReadOnlyDictionary<string, PropertyMutationSpec> explicitMutations,
        string reconstructionClassName)
    {
        if (explicitMutations.TryGetValue(property.Name, out var explicitMutation))
        {
            return PropertyMutationResolution.Success(
                property,
                PropertyMutationStrategyKind.Explicit,
                explicitMutation);
        }

        if (property.CanBeAssigned)
        {
            return PropertyMutationResolution.Success(
                property,
                PropertyMutationStrategyKind.DirectAssignment);
        }

        if (!profile.UseImmutableObjects)
        {
            return PropertyMutationResolution.Failure(
                property,
                "immutable-object conventions are not enabled");
        }

        var generatedMutation = new PropertyMutationSpec(
            configuration.SubjectTypeName,
            property.Name,
            $"global::{configuration.NamespaceName}.{reconstructionClassName}.With{property.Name}");

        if (configuration.IsRecord && property.CanSetInObjectInitializer)
        {
            return PropertyMutationResolution.Success(
                property,
                PropertyMutationStrategyKind.RecordWith,
                generatedMutation);
        }

        var constructor = configuration.ReconstructionConstructors.FirstOrDefault(candidate =>
            candidate.Parameters.Any(parameter => parameter.PropertyName == property.Name));
        if (constructor is not null)
        {
            return PropertyMutationResolution.Success(
                property,
                PropertyMutationStrategyKind.Constructor,
                generatedMutation,
                constructor);
        }

        if (configuration.CanUseDerivedReconstruction && property.CanSetFromDerivedType)
        {
            return PropertyMutationResolution.Success(
                property,
                PropertyMutationStrategyKind.DerivedType,
                generatedMutation);
        }

        return PropertyMutationResolution.Failure(
            property,
            "the type has no usable record 'with', state-preserving constructor, derived-type, or explicit mutation strategy");
    }
}
