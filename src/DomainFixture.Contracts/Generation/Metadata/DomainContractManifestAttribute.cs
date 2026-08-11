using System;
using System.ComponentModel;

namespace DomainFixture.Generation.Metadata;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DomainContractManifestAttribute : Attribute
{
    public int SchemaVersion { get; }
    public Type SourceType { get; }
    public Type SubjectType { get; }
    public string MemberPath { get; }
    public string KindId { get; }
    public string[] Parameters { get; }
    public string? FailureCode { get; }
    public bool PropertyCanBeAssigned { get; }

    public DomainContractManifestAttribute(
        int schemaVersion,
        Type sourceType,
        Type subjectType,
        string memberPath,
        string kindId,
        string[] parameters,
        string? failureCode,
        bool propertyCanBeAssigned)
    {
        SchemaVersion = schemaVersion;
        SourceType = sourceType;
        SubjectType = subjectType;
        MemberPath = memberPath;
        KindId = kindId;
        Parameters = parameters;
        FailureCode = failureCode;
        PropertyCanBeAssigned = propertyCanBeAssigned;
    }
}
