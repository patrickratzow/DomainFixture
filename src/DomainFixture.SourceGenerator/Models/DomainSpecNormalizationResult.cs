using System.Collections.Immutable;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class DomainOperationConflict
{
    public string SubjectTypeName { get; }
    public string OperationId { get; }
    public DomainFact<DomainFixture.Contracts.DomainOperationContract> Existing { get; }
    public DomainFact<DomainFixture.Contracts.DomainOperationContract> Candidate { get; }

    public DomainOperationConflict(
        string subjectTypeName,
        string operationId,
        DomainFact<DomainFixture.Contracts.DomainOperationContract> existing,
        DomainFact<DomainFixture.Contracts.DomainOperationContract> candidate)
    {
        SubjectTypeName = subjectTypeName;
        OperationId = operationId;
        Existing = existing;
        Candidate = candidate;
    }
}

internal sealed class DomainSpecNormalizationResult
{
    public ImmutableArray<DomainTypeSpec> Types { get; }
    public ImmutableArray<DomainOperationConflict> OperationConflicts { get; }
    public ImmutableArray<DomainOperationManifestFailure> ManifestFailures { get; }

    public bool HasConflicts => OperationConflicts.Length > 0 || ManifestFailures.Length > 0;

    public DomainSpecNormalizationResult(
        ImmutableArray<DomainTypeSpec> types,
        ImmutableArray<DomainOperationConflict> operationConflicts,
        ImmutableArray<DomainOperationManifestFailure> manifestFailures)
    {
        Types = types;
        OperationConflicts = operationConflicts;
        ManifestFailures = manifestFailures;
    }
}
