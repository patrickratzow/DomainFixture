namespace DomainFixture.FixtureConfigurations;

public interface IFixturePropertyBuilder<TClass>
{
}

public interface IFixturePropertyBuilder<TClass, TProperty> : IFixturePropertyBuilder<TClass>
{
    public IFixturePropertyBuilder<TClass, TProperty> Valid(TProperty value, string? name = null);
    public IFixturePropertyBuilder<TClass, TProperty> Invalid(TProperty value, string? name = null);
}