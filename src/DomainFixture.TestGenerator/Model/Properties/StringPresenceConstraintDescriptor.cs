using System;

namespace DomainFixture.TestGenerator.Model.Properties;

public enum StringPresenceConstraintKind
{
    NotEmpty,
    NotNull
}

public sealed class StringPresenceConstraintDescriptor
{
    public PropertyDescriptor Property { get; }
    public StringPresenceConstraintKind Kind { get; }
    public string? ErrorCode { get; }

    public StringPresenceConstraintDescriptor(
        PropertyDescriptor property,
        StringPresenceConstraintKind kind,
        string? errorCode = null)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Kind = kind;
        ErrorCode = errorCode;
    }
}
