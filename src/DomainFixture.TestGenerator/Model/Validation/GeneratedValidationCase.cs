using System;

namespace DomainFixture.TestGenerator.Model.Validation;

public sealed class GeneratedValidationCase
{
    public string Name { get; }
    public PropertyMutationDescriptor Mutation { get; }
    public ExpectedValidationOutcome ExpectedOutcome { get; }
    public string? ErrorCode { get; }

    public GeneratedValidationCase(
        string name,
        PropertyMutationDescriptor mutation,
        ExpectedValidationOutcome expectedOutcome,
        string? errorCode = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A validation case must have a name.", nameof(name));

        Name = name;
        Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
        ExpectedOutcome = expectedOutcome;
        ErrorCode = errorCode;
    }
}
