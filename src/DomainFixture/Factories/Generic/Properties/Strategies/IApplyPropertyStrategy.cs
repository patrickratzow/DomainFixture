namespace DomainFixture.Factories.Generic.Properties.Strategies;

public interface IApplyPropertyStrategy
{
    public void Apply<T>(T instance, IObjectProperty objectProperty) where T : notnull;
}