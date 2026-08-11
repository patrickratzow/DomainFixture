using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class ConfiguredValueSpec
{
    public string TypeName { get; }
    public string Expression { get; }
    public Location? Location { get; }

    public ConfiguredValueSpec(
        string typeName,
        string expression,
        Location? location)
    {
        TypeName = typeName;
        Expression = expression;
        Location = location;
    }
}
