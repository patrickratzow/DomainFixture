namespace DomainFixture.TestGenerator.Framework.Strategies;

public class NUnitStrategy : IFrameworkStrategy
{
    public FrameworkAttributes CreateAttributes()
    {
        var testAttribute = new Attribute("TestFixture", "NUnit.Framework");
        var methodAttribute = new Attribute("Test", "NUnit.Framework");

        return new(new[] { methodAttribute }, testAttribute);
    }
}