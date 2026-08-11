using System;
using System.ComponentModel;

namespace DomainFixture.Generation.Metadata;

/// <summary>
/// Declares a compile-time DomainFixture module in a producer assembly. The module implementation
/// is attribution only and is never loaded or instantiated by DomainFixture.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DomainFixtureModuleManifestAttribute : Attribute
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; }
    public string ModuleId { get; }
    public string ModuleVersion { get; }
    public Type SourceType { get; }
    public string[] Capabilities { get; }

    public DomainFixtureModuleManifestAttribute(
        int schemaVersion,
        string moduleId,
        string moduleVersion,
        Type sourceType,
        string[] capabilities)
    {
        SchemaVersion = schemaVersion;
        ModuleId = moduleId;
        ModuleVersion = moduleVersion;
        SourceType = sourceType;
        Capabilities = capabilities ?? Array.Empty<string>();
    }
}
