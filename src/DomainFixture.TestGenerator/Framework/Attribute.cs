using System.Collections.Generic;
using System.Text;

namespace DomainFixture.TestGenerator.Framework;

public record Attribute(string Name, string Namespace, List<string>? Parameters = default)
{
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append('[');
        
        builder.Append(Name);
        if (Parameters is { Count: > 0 })
        {
            foreach (var parameter in Parameters)
            {
                builder.Append(' ');
                builder.Append(parameter);
            }
        }
        
        builder.Append(']');
        
        return builder.ToString();
    }
}