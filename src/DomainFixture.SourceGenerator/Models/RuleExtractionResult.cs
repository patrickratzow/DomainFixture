using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class ConstraintExtractionResult
{
    public DiscoveredDomainConstraint? Constraint { get; }
    public Diagnostic? Diagnostic { get; }

    private ConstraintExtractionResult(DiscoveredDomainConstraint? constraint, Diagnostic? diagnostic)
    {
        Constraint = constraint;
        Diagnostic = diagnostic;
    }

    public static ConstraintExtractionResult Success(DiscoveredDomainConstraint constraint) =>
        new(constraint, null);

    public static ConstraintExtractionResult Failure(Diagnostic diagnostic) => new(null, diagnostic);
}
