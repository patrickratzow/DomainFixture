using System;

namespace DomainFixture.TestGenerator;

public class Indentation : IDisposable
{
    private readonly CodeBuilder _builder;

    public Indentation(CodeBuilder builder)
    {
        _builder = builder;
    }

    public void Dispose()
    {
        _builder.Outdent();
    }
}