using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DomainFixture.SourceGenerator.Models;

internal enum DomainCoverageClassification
{
    Factory,
    Test,
    Both,
    Uncovered
}

internal sealed class DomainCoverageFact
{
    public string SubjectTypeName { get; }
    public string Category { get; }
    public string FactId { get; }
    public DomainCoverageClassification Classification { get; }
    public string Reason { get; }

    public DomainCoverageFact(
        string subjectTypeName,
        string category,
        string factId,
        DomainCoverageClassification classification,
        string reason)
    {
        SubjectTypeName = subjectTypeName;
        Category = category;
        FactId = factId;
        Classification = classification;
        Reason = reason;
    }
}

internal sealed class DomainCoverageReport
{
    public ImmutableArray<DomainCoverageFact> Facts { get; }
    public int Total => Facts.Length;
    public int Factory => Count(DomainCoverageClassification.Factory);
    public int Test => Count(DomainCoverageClassification.Test);
    public int Both => Count(DomainCoverageClassification.Both);
    public int Uncovered => Count(DomainCoverageClassification.Uncovered);

    public DomainCoverageReport(IEnumerable<DomainCoverageFact> facts)
    {
        Facts = facts.ToImmutableArray();
    }

    private int Count(DomainCoverageClassification classification) =>
        Facts.Count(fact => fact.Classification == classification);
}
