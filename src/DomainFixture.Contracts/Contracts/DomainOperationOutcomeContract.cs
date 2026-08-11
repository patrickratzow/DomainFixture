using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DomainFixture.Contracts;

public sealed class DomainOperationOutcomeContract
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; }
    public string OperationId { get; }
    public string KindId { get; }
    public IReadOnlyList<string> Parameters { get; }

    public DomainOperationOutcomeContract(
        string operationId,
        string kindId,
        IReadOnlyList<string> parameters,
        int schemaVersion = CurrentSchemaVersion)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentException("An operation identifier is required.", nameof(operationId));
        if (string.IsNullOrWhiteSpace(kindId))
            throw new ArgumentException("An operation outcome kind identifier is required.", nameof(kindId));
        if (parameters is null)
            throw new ArgumentNullException(nameof(parameters));
        if (kindId == DomainOperationOutcomeKinds.ThrowsException && parameters.Count != 1)
            throw new ArgumentException("An exception outcome must declare exactly one exception type.", nameof(parameters));
        if (kindId == DomainOperationOutcomeKinds.ReturnsResult && parameters.Count != 2)
            throw new ArgumentException(
                "A result outcome must declare success and value member paths.",
                nameof(parameters));
        if (schemaVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));

        var parameterCopy = new List<string>(parameters.Count);
        foreach (var parameter in parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter))
                throw new ArgumentException("Operation outcome parameters cannot contain empty values.", nameof(parameters));

            parameterCopy.Add(parameter);
        }

        SchemaVersion = schemaVersion;
        OperationId = operationId;
        KindId = kindId;
        Parameters = new ReadOnlyCollection<string>(parameterCopy);
    }
}
