using System;

namespace DomainFixture.Generation;

/// <summary>
/// Marks a command argument that should be supplied by DomainFixture's valid-value pipeline.
/// This marker is intended for source-generator analysis and must not be executed directly.
/// </summary>
public static class FixtureValue
{
    public static T Auto<T>() => throw new NotSupportedException(
        "FixtureValue.Auto<T>() is a source-generation marker and cannot be executed directly.");
}
