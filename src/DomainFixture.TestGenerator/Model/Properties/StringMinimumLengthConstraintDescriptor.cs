using System;

namespace DomainFixture.TestGenerator.Model.Properties;

public sealed class StringMinimumLengthConstraintDescriptor
{
    public PropertyDescriptor Property { get; }
    public int Minimum { get; }
    public string? ErrorCode { get; }

    public StringMinimumLengthConstraintDescriptor(
        PropertyDescriptor property,
        int minimum,
        string? errorCode = null)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        if (minimum < 0)
            throw new ArgumentOutOfRangeException(nameof(minimum));

        Minimum = minimum;
        ErrorCode = errorCode;
    }
}
