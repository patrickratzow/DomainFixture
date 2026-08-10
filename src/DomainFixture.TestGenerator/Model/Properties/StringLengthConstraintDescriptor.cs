using System;

namespace DomainFixture.TestGenerator.Model.Properties;

public sealed class StringLengthConstraintDescriptor
{
    public PropertyDescriptor Property { get; }
    public int Minimum { get; }
    public int Maximum { get; }
    public string? ErrorCode { get; }

    public StringLengthConstraintDescriptor(
        PropertyDescriptor property,
        int minimum,
        int maximum,
        string? errorCode = null)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        if (minimum < 0)
            throw new ArgumentOutOfRangeException(nameof(minimum), "A string length cannot be negative.");
        if (maximum < minimum)
            throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum length cannot be below minimum length.");

        Minimum = minimum;
        Maximum = maximum;
        ErrorCode = errorCode;
    }
}
