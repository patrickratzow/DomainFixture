using System.Collections.Immutable;
using DomainFixture.SourceGenerator.Models;
using DomainFixture.SourceGenerator.Modules.FluentValidation;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Modules;

internal sealed class DomainFixtureSourceModulePipeline
{
    private readonly ImmutableArray<IDomainFixtureSourceModule> _modules;

    private DomainFixtureSourceModulePipeline(
        ImmutableArray<IDomainFixtureSourceModule> modules) =>
        _modules = modules;

    public static DomainFixtureSourceModulePipeline CreateDefault() =>
        new(ImmutableArray.Create<IDomainFixtureSourceModule>(
            new FluentValidationSourceModule()));

    public IncrementalValueProvider<ImmutableArray<ConstraintExtractionResult>> Register(
        IncrementalGeneratorInitializationContext context)
    {
        var constraints = context.CompilationProvider.Select(
            static (_, _) => ImmutableArray<ConstraintExtractionResult>.Empty);
        foreach (var module in _modules)
        {
            constraints = constraints.Combine(module.Register(context))
                .Select(static (contributions, _) =>
                    contributions.Left.AddRange(contributions.Right));
        }

        return constraints;
    }
}
