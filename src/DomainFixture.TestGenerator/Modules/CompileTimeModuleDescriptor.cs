using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DomainFixture.TestGenerator.Modules;

/// <summary>
/// Framework-neutral description of a module found in referenced assembly metadata.
/// </summary>
public sealed class CompileTimeModuleDescriptor
{
    public int SchemaVersion { get; }
    public string ModuleId { get; }
    public string ModuleVersion { get; }
    public string SourceTypeName { get; }
    public ImmutableArray<string> Capabilities { get; }

    public CompileTimeModuleDescriptor(
        int schemaVersion,
        string moduleId,
        string moduleVersion,
        string sourceTypeName,
        IEnumerable<string> capabilities)
    {
        SchemaVersion = schemaVersion;
        ModuleId = moduleId ?? string.Empty;
        ModuleVersion = moduleVersion ?? string.Empty;
        SourceTypeName = sourceTypeName ?? string.Empty;
        Capabilities = (capabilities ?? Array.Empty<string>())
            .Where(capability => !string.IsNullOrWhiteSpace(capability))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public bool Supports(string capability) =>
        Capabilities.Contains(capability, StringComparer.Ordinal);
}
