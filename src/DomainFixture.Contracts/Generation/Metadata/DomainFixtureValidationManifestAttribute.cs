using System;
using System.ComponentModel;

namespace DomainFixture.Generation.Metadata;

/// <summary>
/// Binds a subject to a module-generated IFixtureValidator adapter. The adapter type is transported
/// as metadata text so another generator in the consuming compilation may emit it concurrently.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DomainFixtureValidationManifestAttribute : Attribute
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; }
    public string ModuleId { get; }
    public Type SourceType { get; }
    public Type SubjectType { get; }
    public string AdapterTypeName { get; }

    public DomainFixtureValidationManifestAttribute(
        int schemaVersion,
        string moduleId,
        Type sourceType,
        Type subjectType,
        string adapterTypeName)
    {
        SchemaVersion = schemaVersion;
        ModuleId = moduleId;
        SourceType = sourceType;
        SubjectType = subjectType;
        AdapterTypeName = adapterTypeName;
    }
}
