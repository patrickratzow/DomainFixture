using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace DomainFixture.Contracts;

public sealed class DomainConstraintContract
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; }
    public string KindId { get; }
    public string SourceTypeName { get; }
    public string SubjectTypeName { get; }
    public string MemberPath { get; }
    public IReadOnlyDictionary<string, string> Parameters { get; }
    public string? FailureCode { get; }

    public DomainConstraintContract(
        string kindId,
        string sourceTypeName,
        string subjectTypeName,
        string memberPath,
        IReadOnlyDictionary<string, string>? parameters = null,
        string? failureCode = null,
        int schemaVersion = CurrentSchemaVersion)
    {
        if (string.IsNullOrWhiteSpace(kindId))
            throw new ArgumentException("A contract kind identifier is required.", nameof(kindId));
        if (string.IsNullOrWhiteSpace(sourceTypeName))
            throw new ArgumentException("A source type is required.", nameof(sourceTypeName));
        if (string.IsNullOrWhiteSpace(subjectTypeName))
            throw new ArgumentException("A subject type is required.", nameof(subjectTypeName));
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("A member path is required.", nameof(memberPath));
        if (schemaVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));

        var parameterCopy = new Dictionary<string, string>(StringComparer.Ordinal);
        if (parameters is not null)
        {
            foreach (var parameter in parameters)
                parameterCopy.Add(parameter.Key, parameter.Value);
        }

        SchemaVersion = schemaVersion;
        KindId = kindId;
        SourceTypeName = sourceTypeName;
        SubjectTypeName = subjectTypeName;
        MemberPath = memberPath;
        Parameters = new ReadOnlyDictionary<string, string>(parameterCopy);
        FailureCode = failureCode;
    }

    public bool TryGetInt32(string name, out int value)
    {
        value = default;
        return Parameters.TryGetValue(name, out var raw) &&
               int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
