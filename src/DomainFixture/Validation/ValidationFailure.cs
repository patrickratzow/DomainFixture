using System;

namespace DomainFixture.Validation;

public sealed class ValidationFailure
{
    public string Property { get; }
    public string? Code { get; }
    public string? Message { get; }

    public ValidationFailure(string property, string? code = null, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("A validation failure must identify a property.", nameof(property));

        Property = property;
        Code = code;
        Message = message;
    }
}
