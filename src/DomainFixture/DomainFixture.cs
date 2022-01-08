using System;

namespace DomainFixture;

public static class DomainFixture
{
    public static IFixtureBuilder<TFixture> Valid<TFixture>()
    {
        throw new NotImplementedException();
    }

    public static IFixtureBuilder<TFixture> Invalid<TFixture>()
    {
        throw new NotImplementedException();
    }
}