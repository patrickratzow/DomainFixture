using System.Collections.Immutable;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class SubjectConstructorSpec
{
    public ImmutableArray<ConstructorParameterSpec> Parameters { get; }

    public SubjectConstructorSpec(ImmutableArray<ConstructorParameterSpec> parameters)
    {
        Parameters = parameters;
    }
}

internal sealed class ConstructorParameterSpec
{
    public string PropertyName { get; }

    public ConstructorParameterSpec(string propertyName)
    {
        PropertyName = propertyName;
    }
}
