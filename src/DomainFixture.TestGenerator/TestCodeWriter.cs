using System;
using DomainFixture.TestGenerator.Framework;

namespace DomainFixture.TestGenerator;

public class TestCodeWriter<TClass> : CodeBuilder
{
    private readonly TestingFramework _testingFramework;
    public string ClassName => $"{typeof(TClass).Name}Tests";
    public string NamespaceName { get; }

    public TestCodeWriter(string namespaceName, TestingFramework testingFramework)
    {
        _testingFramework = testingFramework;
        NamespaceName = namespaceName;
    }

    protected override void Write()
    {
        throw new NotImplementedException();
    }
}