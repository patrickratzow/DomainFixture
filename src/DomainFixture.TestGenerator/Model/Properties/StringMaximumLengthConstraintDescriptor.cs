using System;

namespace DomainFixture.TestGenerator.Model.Properties;

public sealed class StringMaximumLengthConstraintDescriptor
{
    public PropertyDescriptor Property { get; }
    public int Maximum { get; }
    public string? ErrorCode { get; }

    public StringMaximumLengthConstraintDescriptor(
        PropertyDescriptor property,
        int maximum,
        string? errorCode = null)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        if (maximum < 0)
            throw new ArgumentOutOfRangeException(nameof(maximum));

        Maximum = maximum;
        ErrorCode = errorCode;
    }
}
