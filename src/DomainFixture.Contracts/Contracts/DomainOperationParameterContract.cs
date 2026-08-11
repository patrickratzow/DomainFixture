using System;

namespace DomainFixture.Contracts;

public sealed class DomainOperationParameterContract
{
    public string Name { get; }
    public string TypeName { get; }
    public string MemberPath { get; }

    public DomainOperationParameterContract(
        string name,
        string typeName,
        string memberPath)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("An operation parameter name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("An operation parameter type is required.", nameof(typeName));
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("An operation parameter member path is required.", nameof(memberPath));

        Name = name;
        TypeName = typeName;
        MemberPath = memberPath;
    }
}
