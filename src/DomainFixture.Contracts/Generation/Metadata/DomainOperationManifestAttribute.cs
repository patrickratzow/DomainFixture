using System;
using System.ComponentModel;

namespace DomainFixture.Generation.Metadata;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DomainOperationManifestAttribute : Attribute
{
    public int SchemaVersion { get; }
    public Type SourceType { get; }
    public string OperationId { get; }
    public string KindId { get; }
    public Type DeclaringType { get; }
    public Type SubjectType { get; }
    public string MemberName { get; }
    public Type ReturnType { get; }
    public string[] ParameterNames { get; }
    public Type[] ParameterTypes { get; }
    public string[] MemberPaths { get; }

    public DomainOperationManifestAttribute(
        int schemaVersion,
        Type sourceType,
        string operationId,
        string kindId,
        Type declaringType,
        Type subjectType,
        string memberName,
        Type returnType,
        string[] parameterNames,
        Type[] parameterTypes,
        string[] memberPaths)
    {
        SchemaVersion = schemaVersion;
        SourceType = sourceType;
        OperationId = operationId;
        KindId = kindId;
        DeclaringType = declaringType;
        SubjectType = subjectType;
        MemberName = memberName;
        ReturnType = returnType;
        ParameterNames = parameterNames;
        ParameterTypes = parameterTypes;
        MemberPaths = memberPaths;
    }
}
