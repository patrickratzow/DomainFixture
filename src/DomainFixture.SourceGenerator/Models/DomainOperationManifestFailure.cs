namespace DomainFixture.SourceGenerator.Models;

internal sealed class DomainOperationManifestFailure
{
    public DomainOperationManifestFailureKind Kind { get; }
    public string OperationId { get; }
    public string Message { get; }

    public DomainOperationManifestFailure(
        DomainOperationManifestFailureKind kind,
        string operationId,
        string message)
    {
        Kind = kind;
        OperationId = operationId;
        Message = message;
    }
}
