using System.Collections.Immutable;
using DomainFixture.Modules.FluentValidation;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Modules.FluentValidation;

internal sealed class FluentValidationSourceModule : IDomainFixtureSourceModule
{
    private readonly FluentValidationModule _module = new();

    public string Id => _module.Id;

    public IncrementalValueProvider<ImmutableArray<ConstraintExtractionResult>> Register(
        IncrementalGeneratorInitializationContext context)
    {
        _module.Register(context);

        return FluentValidationConstraintProvider.Create(context).Collect();
    }
}
