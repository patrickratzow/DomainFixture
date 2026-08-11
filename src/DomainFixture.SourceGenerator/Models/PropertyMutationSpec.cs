namespace DomainFixture.SourceGenerator.Models;

internal sealed class PropertyMutationSpec
{
    public string SubjectTypeKey { get; }
    public string PropertyName { get; }
    public string ReconstructionExpression { get; }

    public PropertyMutationSpec(
        string subjectTypeKey,
        string propertyName,
        string reconstructionExpression)
    {
        SubjectTypeKey = subjectTypeKey;
        PropertyName = propertyName;
        ReconstructionExpression = reconstructionExpression;
    }
}
