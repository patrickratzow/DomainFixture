using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class ValidationRuleSpec
{
    public string ValidationRulesTypeKey { get; }
    public string PropertyName { get; }
    public ValidationRuleKind Kind { get; }
    public int? Minimum { get; }
    public int? Maximum { get; }
    public string? ErrorCode { get; }
    public bool PropertyCanBeAssigned { get; }
    public Location? Location { get; }

    public ValidationRuleSpec(
        string validationRulesTypeKey,
        string propertyName,
        ValidationRuleKind kind,
        int? minimum,
        int? maximum,
        string? errorCode,
        bool propertyCanBeAssigned,
        Location? location)
    {
        ValidationRulesTypeKey = validationRulesTypeKey;
        PropertyName = propertyName;
        Kind = kind;
        Minimum = minimum;
        Maximum = maximum;
        ErrorCode = errorCode;
        PropertyCanBeAssigned = propertyCanBeAssigned;
        Location = location;
    }
}
