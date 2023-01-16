namespace DomainFixture.Factories;

public interface IObjectValue<out T>
{
    public T Value { get; }
}

public interface IObjectValue : IObjectValue<object>
{
}