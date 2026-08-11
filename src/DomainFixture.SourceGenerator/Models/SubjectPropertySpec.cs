namespace DomainFixture.SourceGenerator.Models;

internal sealed class SubjectPropertySpec
{
    public string Name { get; }
    public bool IsString { get; }
    public bool IsNonNullable { get; }
    public bool CanBeAssigned { get; }

    public SubjectPropertySpec(
        string name,
        bool isString,
        bool isNonNullable,
        bool canBeAssigned)
    {
        Name = name;
        IsString = isString;
        IsNonNullable = isNonNullable;
        CanBeAssigned = canBeAssigned;
    }
}
