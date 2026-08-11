namespace DomainFixture.Modules.FluentValidation;

internal sealed class RuleContribution
{
    public string SubjectTypeName { get; }
    public string PropertyName { get; }
    public string KindId { get; }
    public int? Minimum { get; }
    public int? Maximum { get; }
    public string? ErrorCode { get; }
    public bool PropertyCanBeAssigned { get; }
    public string Identity =>
        PropertyName + "|" + KindId + "|" + Minimum + "|" + Maximum + "|" + ErrorCode;

    public RuleContribution(
        string subjectTypeName,
        string propertyName,
        string kindId,
        int? minimum,
        int? maximum,
        string? errorCode,
        bool propertyCanBeAssigned)
    {
        SubjectTypeName = subjectTypeName;
        PropertyName = propertyName;
        KindId = kindId;
        Minimum = minimum;
        Maximum = maximum;
        ErrorCode = errorCode;
        PropertyCanBeAssigned = propertyCanBeAssigned;
    }
}
