using System;
using System.Collections.Generic;
using System.Linq;

namespace DomainFixture.Validation;

public sealed class ValidationReport
{
    public static ValidationReport Valid { get; } = new(Array.Empty<ValidationFailure>());

    public IReadOnlyList<ValidationFailure> Failures { get; }
    public bool IsValid => Failures.Count == 0;

    public ValidationReport(IEnumerable<ValidationFailure> failures)
    {
        if (failures is null) throw new ArgumentNullException(nameof(failures));

        Failures = failures.ToArray();
    }

    public bool ContainsFailure(string property, string? code = null)
    {
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("A property is required.", nameof(property));

        return Failures.Any(failure =>
            string.Equals(failure.Property, property, StringComparison.Ordinal) &&
            (code is null || string.Equals(failure.Code, code, StringComparison.Ordinal)));
    }
}
