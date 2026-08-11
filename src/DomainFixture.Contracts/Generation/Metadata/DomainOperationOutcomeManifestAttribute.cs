using System;
using System.ComponentModel;

namespace DomainFixture.Generation.Metadata;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DomainOperationOutcomeManifestAttribute : Attribute
{
    public int SchemaVersion { get; }
    public Type SourceType { get; }
    public Type SubjectType { get; }
    public string OperationId { get; }
    public string KindId { get; }
    public string[] Parameters { get; }

    public DomainOperationOutcomeManifestAttribute(
        int schemaVersion,
        Type sourceType,
        Type subjectType,
        string operationId,
        string kindId,
        string[] parameters)
    {
        SchemaVersion = schemaVersion;
        SourceType = sourceType;
        SubjectType = subjectType;
        OperationId = operationId;
        KindId = kindId;
        Parameters = parameters;
    }
}
