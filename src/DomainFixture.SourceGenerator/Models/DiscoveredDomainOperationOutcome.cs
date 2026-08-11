using DomainFixture.Contracts;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class DiscoveredDomainOperationOutcome
{
    public DomainOperationOutcomeContract Contract { get; }
    public string SourceTypeName { get; }
    public string SubjectTypeName { get; }
    public Location? Location { get; }

    public DiscoveredDomainOperationOutcome(
        DomainOperationOutcomeContract contract,
        string sourceTypeName,
        string subjectTypeName,
        Location? location)
    {
        Contract = contract;
        SourceTypeName = sourceTypeName;
        SubjectTypeName = subjectTypeName;
        Location = location;
    }
}
