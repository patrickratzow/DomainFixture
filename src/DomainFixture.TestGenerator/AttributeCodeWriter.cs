using System;
using System.Collections.Generic;
using Attribute = DomainFixture.TestGenerator.Framework.Attribute;

namespace DomainFixture.TestGenerator;

public class AttributeCodeWriter : CodeBuilder
{
    private readonly List<Attribute> _attributes = new();
    
    public AttributeCodeWriter AddAttributes(IEnumerable<Attribute> attributes)
    {
        _attributes.AddRange(attributes);

        return this;
    }

    protected override void Write()
    {
        var count = _attributes.Count;
        for (var i = 0; i < _attributes.Count; i++)
        {
            var attribute = _attributes[i];
            Append(attribute.ToString());
            
            if (i != count - 1) 
                Append(Environment.NewLine);
        }
    }
}