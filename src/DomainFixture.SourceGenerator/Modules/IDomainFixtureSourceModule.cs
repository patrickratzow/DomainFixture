using System.Collections.Immutable;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Modules;

internal interface IDomainFixtureSourceModule
{
    string Id { get; }

    IncrementalValueProvider<ImmutableArray<ConstraintExtractionResult>> Register(
        IncrementalGeneratorInitializationContext context);
}
