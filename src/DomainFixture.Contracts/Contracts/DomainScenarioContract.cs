using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DomainFixture.Contracts;

public sealed class DomainScenarioContract
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; }
    public string KindId { get; }
    public string SourceTypeName { get; }
    public string SubjectTypeName { get; }
    public IReadOnlyDictionary<string, string> Parameters { get; }

    public DomainScenarioContract(
        string kindId,
        string sourceTypeName,
        string subjectTypeName,
        IReadOnlyDictionary<string, string>? parameters = null,
        int schemaVersion = CurrentSchemaVersion)
    {
        if (string.IsNullOrWhiteSpace(kindId))
            throw new ArgumentException("A scenario kind identifier is required.", nameof(kindId));
        if (string.IsNullOrWhiteSpace(sourceTypeName))
            throw new ArgumentException("A scenario source type is required.", nameof(sourceTypeName));
        if (string.IsNullOrWhiteSpace(subjectTypeName))
            throw new ArgumentException("A scenario subject type is required.", nameof(subjectTypeName));
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
        Parameters = new ReadOnlyDictionary<string, string>(parameterCopy);
    }
}
