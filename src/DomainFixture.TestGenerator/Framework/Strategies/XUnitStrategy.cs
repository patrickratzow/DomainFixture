namespace DomainFixture.TestGenerator.Framework.Strategies;

public class XUnitStrategy : IFrameworkStrategy
{
    public FrameworkAttributes CreateAttributes()
    {
        var methodAttribute = new Attribute("Fact", "Xunit");

        return new(new[] { methodAttribute });
    }
}