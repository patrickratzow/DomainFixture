using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class OperationRejectionSpec
{
    public string? SubjectTypeKey { get; }
    public string ExceptionTypeName { get; }
    public Location? Location { get; }

    public OperationRejectionSpec(
        string? subjectTypeKey,
        string exceptionTypeName,
        Location? location)
    {
        SubjectTypeKey = subjectTypeKey;
        ExceptionTypeName = exceptionTypeName;
        Location = location;
    }
}
