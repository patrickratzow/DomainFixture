namespace DomainFixture.SourceGenerator.Models;

internal sealed class SubjectPropertySpec
{
    public string Name { get; }
    public string TypeName { get; }
    public bool IsString { get; }
    public bool IsNonNullable { get; }
    public bool CanBeAssigned { get; }
    public bool HasSetter { get; }
    public bool CanSetFromDerivedType { get; }
    public bool CanReadFromGeneratedCode { get; }

    public SubjectPropertySpec(
        string name,
        string typeName,
        bool isString,
        bool isNonNullable,
        bool canBeAssigned,
        bool hasSetter,
        bool canSetFromDerivedType,
        bool canReadFromGeneratedCode)
    {
        Name = name;
        TypeName = typeName;
        IsString = isString;
        IsNonNullable = isNonNullable;
        CanBeAssigned = canBeAssigned;
        HasSetter = hasSetter;
        CanSetFromDerivedType = canSetFromDerivedType;
        CanReadFromGeneratedCode = canReadFromGeneratedCode;
    }
}
