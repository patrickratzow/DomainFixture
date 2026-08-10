using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class ConfigurationParseResult
{
    public ImmutableArray<FixtureGenerationSpec> Configurations { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public ConfigurationParseResult(
        ImmutableArray<FixtureGenerationSpec> configurations,
        ImmutableArray<Diagnostic> diagnostics)
    {
        Configurations = configurations;
        Diagnostics = diagnostics;
    }
}
