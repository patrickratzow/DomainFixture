using System;
using System.Collections.Generic;
using Attribute = DomainFixture.TestGenerator.Framework.Attribute;

namespace DomainFixture.TestGenerator;

public class TestCodeWriter<TClass> : CodeBuilder
{
    private readonly Framework.TestingFramework _testTestingFramework;
    public string ClassName => $"{typeof(TClass).Name}Tests";
    public string NamespaceName { get; }

    public TestCodeWriter(string namespaceName, Framework.TestingFramework testTestingFramework)
    {
        _testTestingFramework = testTestingFramework;
        NamespaceName = namespaceName;
    }

    protected override void Write()
    {
        throw new System.NotImplementedException();
    }
}

public class AttributeCodeWriter : CodeBuilder
{
    private readonly List<Attribute> _attributes = new();
    
    public AttributeCodeWriter AddAttributes(params Attribute[] attributes)
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
