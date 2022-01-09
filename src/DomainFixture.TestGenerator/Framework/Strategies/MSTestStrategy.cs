namespace DomainFixture.TestGenerator.Framework.Strategies;

// ReSharper disable once InconsistentNaming
public class MSTestStrategy : IFrameworkStrategy
{
    public FrameworkAttributes CreateAttributes()
    {
        var testAttribute = new Attribute("TestClass", "Microsoft.VisualStudio.TestTools.UnitTesting");
        var methodAttribute = new Attribute("TestMethod", "Microsoft.VisualStudio.TestTools.UnitTesting");

        return new(new[] { methodAttribute }, testAttribute);
    }
}