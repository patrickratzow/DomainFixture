using System;
using System.ComponentModel;

namespace DomainFixture.Generation.Metadata;

[EditorBrowsable(EditorBrowsableState.Never)]
public enum ValidationRuleManifestKind
{
    StringLength,
    NotEmpty,
    NotNull,
    StringMaximumLength
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ValidationRuleManifestAttribute : Attribute
{
    public Type ValidationRulesType { get; }
    public Type SubjectType { get; }
    public string PropertyName { get; }
    public ValidationRuleManifestKind Kind { get; }
    public int Minimum { get; }
    public int Maximum { get; }
    public string? ErrorCode { get; }
    public bool PropertyCanBeAssigned { get; }

    public ValidationRuleManifestAttribute(
        Type validationRulesType,
        Type subjectType,
        string propertyName,
        ValidationRuleManifestKind kind,
        int minimum,
        int maximum,
        string? errorCode,
        bool propertyCanBeAssigned)
    {
        ValidationRulesType = validationRulesType;
        SubjectType = subjectType;
        PropertyName = propertyName;
        Kind = kind;
        Minimum = minimum;
        Maximum = maximum;
        ErrorCode = errorCode;
        PropertyCanBeAssigned = propertyCanBeAssigned;
    }
}
