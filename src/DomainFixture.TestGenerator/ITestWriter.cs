using System.Collections.Generic;

namespace DomainFixture.TestGenerator;

public interface ITestWriter
{
    public List<string> Namespaces { get; }
    public List<string> Tests { get; }
}

public class TestWriter<TClass, TProperty> : ITestWriter
{

    public List<string> Namespaces { get; }
    public List<string> Tests { get; }
}