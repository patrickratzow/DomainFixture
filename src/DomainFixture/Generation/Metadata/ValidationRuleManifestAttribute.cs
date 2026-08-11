using System;
using System.ComponentModel;

namespace DomainFixture.Generation.Metadata;

[EditorBrowsable(EditorBrowsableState.Never)]
public enum ValidationRuleManifestKind
{
    TextLength = 0,
    StringLength = TextLength,
    NotEmpty = 1,
    NotNull = 2,
    TextMaximumLength = 3,
    StringMaximumLength = TextMaximumLength,
    TextMinimumLength = 4,
    Int32InclusiveRange = 5,
    Int32ExclusiveRange = 6,
    Int32GreaterThan = 7,
    Int32LessThan = 8
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
