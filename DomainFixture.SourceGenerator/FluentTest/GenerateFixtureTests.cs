using System;

namespace DomainFixture.SourceGenerator;

public class GenerateFixtureTests : Attribute
{
    private readonly Type _type;

    public GenerateFixtureTests(Type type)
    {
        _type = type;
    }
}