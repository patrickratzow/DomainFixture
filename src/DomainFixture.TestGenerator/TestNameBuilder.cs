using DomainFixture.FixtureConfigurations;

namespace DomainFixture.TestGenerator;

public class TestNameBuilder<TClass, TProperty, TPropertyBuilder> 
    where TPropertyBuilder : IFixturePropertyBuilder<TClass, TProperty>
{
    private readonly TPropertyBuilder _propertyBuilder;

    public TestNameBuilder(TPropertyBuilder propertyBuilder)
    {
        _propertyBuilder = propertyBuilder;
    }
}