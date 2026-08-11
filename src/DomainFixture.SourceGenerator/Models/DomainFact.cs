using System;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal enum DomainFactProvenance
{
    Inferred,
    Discovered,
    Manifest,
    Profile
}

internal sealed class DomainFact<T>
{
    public T Value { get; }
    public DomainFactProvenance Provenance { get; }
    public string SourceId { get; }
    public Location? Location { get; }

    public DomainFact(
        T value,
        DomainFactProvenance provenance,
        string sourceId,
        Location? location)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("A domain fact source identifier is required.", nameof(sourceId));

        Provenance = provenance;
        SourceId = sourceId;
        Location = location;
    }
}
