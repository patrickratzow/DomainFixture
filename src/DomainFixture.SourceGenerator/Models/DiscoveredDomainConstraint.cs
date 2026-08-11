using System.Collections.Generic;
using System.Globalization;
using DomainFixture.Contracts;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class DiscoveredDomainConstraint
{
    public DomainConstraintContract Contract { get; }
    public bool PropertyCanBeAssigned { get; }
    public Location? Location { get; }

    public string SourceTypeKey => Contract.SourceTypeName;
    public string SubjectTypeKey => Contract.SubjectTypeName;
    public string PropertyName => Contract.MemberPath;
    public string KindId => Contract.KindId;
    public string? ErrorCode => Contract.FailureCode;
    public int? Minimum => ReadInt32(DomainConstraintParameters.Minimum);
    public int? Maximum => ReadInt32(DomainConstraintParameters.Maximum);

    public DiscoveredDomainConstraint(
        string sourceTypeKey,
        string subjectTypeKey,
        string propertyName,
        string kindId,
        int? minimum,
        int? maximum,
        string? errorCode,
        bool propertyCanBeAssigned,
        Location? location,
        int schemaVersion = DomainConstraintContract.CurrentSchemaVersion)
    {
        var parameters = new Dictionary<string, string>();
        if (minimum is not null)
        {
            parameters.Add(
                DomainConstraintParameters.Minimum,
                minimum.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (maximum is not null)
        {
            parameters.Add(
                DomainConstraintParameters.Maximum,
                maximum.Value.ToString(CultureInfo.InvariantCulture));
        }

        Contract = new DomainConstraintContract(
            kindId,
            sourceTypeKey,
            subjectTypeKey,
            propertyName,
            parameters,
            errorCode,
            schemaVersion);
        PropertyCanBeAssigned = propertyCanBeAssigned;
        Location = location;
    }

    public DiscoveredDomainConstraint(
        DomainConstraintContract contract,
        bool propertyCanBeAssigned,
        Location? location)
    {
        Contract = contract;
        PropertyCanBeAssigned = propertyCanBeAssigned;
        Location = location;
    }

    private int? ReadInt32(string name) =>
        Contract.TryGetInt32(name, out var value) ? value : null;
}
