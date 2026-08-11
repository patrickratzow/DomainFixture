using System;

namespace DomainFixture.TestGenerator.Model.Properties;

public sealed class Int32RangeConstraintDescriptor
{
    public PropertyDescriptor Property { get; }
    public int? Minimum { get; }
    public bool MinimumInclusive { get; }
    public int? Maximum { get; }
    public bool MaximumInclusive { get; }
    public string? ErrorCode { get; }

    public Int32RangeConstraintDescriptor(
        PropertyDescriptor property,
        int? minimum,
        bool minimumInclusive,
        int? maximum,
        bool maximumInclusive,
        string? errorCode = null)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        if (minimum is null && maximum is null)
            throw new ArgumentException("At least one range boundary is required.");
        if (minimum is not null && maximum is not null && maximum < minimum)
            throw new ArgumentOutOfRangeException(nameof(maximum));

        Minimum = minimum;
        MinimumInclusive = minimumInclusive;
        Maximum = maximum;
        MaximumInclusive = maximumInclusive;
        ErrorCode = errorCode;
    }
}
