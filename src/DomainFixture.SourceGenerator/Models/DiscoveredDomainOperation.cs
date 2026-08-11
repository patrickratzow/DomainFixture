using DomainFixture.Contracts;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class DiscoveredDomainOperation
{
    public DomainOperationContract Contract { get; }
    public string SourceTypeName { get; }
    public Location? Location { get; }
    public ITypeSymbol? ReturnTypeSymbol { get; }

    public DiscoveredDomainOperation(
        DomainOperationContract contract,
        string sourceTypeName,
        Location? location,
        ITypeSymbol? returnTypeSymbol = null)
    {
        Contract = contract;
        SourceTypeName = sourceTypeName;
        Location = location;
        ReturnTypeSymbol = returnTypeSymbol;
    }
}
