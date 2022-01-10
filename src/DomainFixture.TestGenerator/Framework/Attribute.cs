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
            builder.Append('(');
            for (var i = 0; i < Parameters.Count; i++)
            {
                if (i != 0) 
                    builder.Append(' ');
                
                var parameter = Parameters[i];
                builder.Append(parameter);
            }
            builder.Append(')');
        }
        
        builder.Append(']');
        
        return builder.ToString();
    }
}