using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DomainFixture.Contracts;

public sealed class DomainOperationContract
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; }
    public string OperationId { get; }
    public string KindId { get; }
    public string DeclaringTypeName { get; }
    public string SubjectTypeName { get; }
    public string MemberName { get; }
    public string ReturnTypeName { get; }
    public IReadOnlyList<DomainOperationParameterContract> Parameters { get; }

    public DomainOperationContract(
        string kindId,
        string declaringTypeName,
        string subjectTypeName,
        string memberName,
        IReadOnlyList<DomainOperationParameterContract> parameters,
        int schemaVersion = CurrentSchemaVersion)
        : this(
            CreateOperationId(kindId, declaringTypeName, memberName, parameters),
            kindId,
            declaringTypeName,
            subjectTypeName,
            memberName,
            subjectTypeName,
            parameters,
            schemaVersion)
    {
    }

    public DomainOperationContract(
        string operationId,
        string kindId,
        string declaringTypeName,
        string subjectTypeName,
        string memberName,
        string returnTypeName,
        IReadOnlyList<DomainOperationParameterContract> parameters,
        int schemaVersion = CurrentSchemaVersion)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentException("An operation identifier is required.", nameof(operationId));
        if (string.IsNullOrWhiteSpace(kindId))
            throw new ArgumentException("An operation kind identifier is required.", nameof(kindId));
        if (string.IsNullOrWhiteSpace(declaringTypeName))
            throw new ArgumentException("An operation declaring type is required.", nameof(declaringTypeName));
        if (string.IsNullOrWhiteSpace(subjectTypeName))
            throw new ArgumentException("An operation subject type is required.", nameof(subjectTypeName));
        if (string.IsNullOrWhiteSpace(memberName))
            throw new ArgumentException("An operation member name is required.", nameof(memberName));
        if (string.IsNullOrWhiteSpace(returnTypeName))
            throw new ArgumentException("An operation return type is required.", nameof(returnTypeName));
        if (parameters is null)
            throw new ArgumentNullException(nameof(parameters));
        if (kindId != DomainOperationKinds.InstanceCommand && parameters.Count == 0)
            throw new ArgumentException("A construction operation must declare at least one parameter.", nameof(parameters));
        if (schemaVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));

        var parameterCopy = new List<DomainOperationParameterContract>(parameters.Count);
        var parameterNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameter in parameters)
        {
            if (parameter is null)
                throw new ArgumentException("Operation parameters cannot contain null values.", nameof(parameters));
            if (!parameterNames.Add(parameter.Name))
                throw new ArgumentException($"Operation parameter '{parameter.Name}' is duplicated.", nameof(parameters));

            parameterCopy.Add(parameter);
        }

        SchemaVersion = schemaVersion;
        OperationId = operationId;
        KindId = kindId;
        DeclaringTypeName = declaringTypeName;
        SubjectTypeName = subjectTypeName;
        MemberName = memberName;
        ReturnTypeName = returnTypeName;
        Parameters = new ReadOnlyCollection<DomainOperationParameterContract>(parameterCopy);
    }

    public static string CreateOperationId(
        string kindId,
        string declaringTypeName,
        string memberName,
        IReadOnlyList<DomainOperationParameterContract> parameters)
    {
        if (string.IsNullOrWhiteSpace(kindId))
            throw new ArgumentException("An operation kind identifier is required.", nameof(kindId));
        if (string.IsNullOrWhiteSpace(declaringTypeName))
            throw new ArgumentException("An operation declaring type is required.", nameof(declaringTypeName));
        if (string.IsNullOrWhiteSpace(memberName))
            throw new ArgumentException("An operation member name is required.", nameof(memberName));
        if (parameters is null)
            throw new ArgumentNullException(nameof(parameters));

        var parameterTypes = new string[parameters.Count];
        for (var index = 0; index < parameters.Count; index++)
        {
            var parameter = parameters[index];
            if (parameter is null)
                throw new ArgumentException("Operation parameters cannot contain null values.", nameof(parameters));

            parameterTypes[index] = parameter.TypeName;
        }

        return $"{kindId}:{declaringTypeName}.{memberName}({string.Join(",", parameterTypes)})";
    }
}
