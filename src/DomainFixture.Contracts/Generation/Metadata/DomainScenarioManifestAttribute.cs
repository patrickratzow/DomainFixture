using System;
using System.ComponentModel;

namespace DomainFixture.Generation.Metadata;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DomainScenarioManifestAttribute : Attribute
{
    public int SchemaVersion { get; }
    public Type SourceType { get; }
    public Type SubjectType { get; }
    public string KindId { get; }
    public string[] Parameters { get; }

    public DomainScenarioManifestAttribute(
        int schemaVersion,
        Type sourceType,
        Type subjectType,
        string kindId,
        string[] parameters)
    {
        SchemaVersion = schemaVersion;
        SourceType = sourceType;
        SubjectType = subjectType;
        KindId = kindId;
        Parameters = parameters;
    }
}
