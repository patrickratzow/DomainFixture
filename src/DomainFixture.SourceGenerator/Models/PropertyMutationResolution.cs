namespace DomainFixture.SourceGenerator.Models;

internal sealed class PropertyMutationResolution
{
    public SubjectPropertySpec Property { get; }
    public PropertyMutationStrategyKind? Strategy { get; }
    public PropertyMutationSpec? Mutation { get; }
    public SubjectConstructorSpec? Constructor { get; }
    public string? FailureReason { get; }

    private PropertyMutationResolution(
        SubjectPropertySpec property,
        PropertyMutationStrategyKind? strategy,
        PropertyMutationSpec? mutation,
        SubjectConstructorSpec? constructor,
        string? failureReason)
    {
        Property = property;
        Strategy = strategy;
        Mutation = mutation;
        Constructor = constructor;
        FailureReason = failureReason;
    }

    public static PropertyMutationResolution Success(
        SubjectPropertySpec property,
        PropertyMutationStrategyKind strategy,
        PropertyMutationSpec? mutation = null,
        SubjectConstructorSpec? constructor = null) =>
        new(property, strategy, mutation, constructor, failureReason: null);

    public static PropertyMutationResolution Failure(
        SubjectPropertySpec property,
        string reason) =>
        new(property, strategy: null, mutation: null, constructor: null, reason);
}
