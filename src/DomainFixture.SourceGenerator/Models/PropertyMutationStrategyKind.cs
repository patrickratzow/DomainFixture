namespace DomainFixture.SourceGenerator.Models;

internal enum PropertyMutationStrategyKind
{
    Explicit,
    DirectAssignment,
    RecordWith,
    Constructor,
    DerivedType
}
