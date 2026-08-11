using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class GenerationProfileParseResult
{
    public GenerationProfileSpec Profile { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public GenerationProfileParseResult(
        GenerationProfileSpec profile,
        ImmutableArray<Diagnostic> diagnostics)
    {
        Profile = profile;
        Diagnostics = diagnostics;
    }
}
