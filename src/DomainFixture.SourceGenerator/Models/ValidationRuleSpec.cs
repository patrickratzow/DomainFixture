using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class ValidationRuleSpec
{
    public string ValidationRulesTypeKey { get; }
    public string PropertyName { get; }
    public int Minimum { get; }
    public int Maximum { get; }
    public string? ErrorCode { get; }
    public Location? Location { get; }

    public ValidationRuleSpec(
        string validationRulesTypeKey,
        string propertyName,
        int minimum,
        int maximum,
        string? errorCode,
        Location? location)
    {
        ValidationRulesTypeKey = validationRulesTypeKey;
        PropertyName = propertyName;
        Minimum = minimum;
        Maximum = maximum;
        ErrorCode = errorCode;
        Location = location;
    }
}
