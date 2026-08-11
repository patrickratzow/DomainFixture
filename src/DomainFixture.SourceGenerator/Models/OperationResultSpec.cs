using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class OperationResultSpec
{
    public string SubjectTypeName { get; }
    public string ResultTypeName { get; }
    public string SuccessMemberPath { get; }
    public string ValueMemberPath { get; }
    public Location? Location { get; }

    public OperationResultSpec(
        string subjectTypeName,
        string resultTypeName,
        string successMemberPath,
        string valueMemberPath,
        Location? location)
    {
        SubjectTypeName = subjectTypeName;
        ResultTypeName = resultTypeName;
        SuccessMemberPath = successMemberPath;
        ValueMemberPath = valueMemberPath;
        Location = location;
    }
}
