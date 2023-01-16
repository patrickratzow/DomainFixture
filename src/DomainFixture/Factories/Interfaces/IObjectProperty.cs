namespace DomainFixture.Factories;

public interface IObjectProperty<out T>
{
    public IObjectKey Key { get; }
    public IObjectValue<T> Value { get; }
}

public interface IObjectProperty : IObjectProperty<object>
{
}