namespace DomainFixture.Factories;

public interface IObjectFactory<out T> where T : notnull
{
    public T Create(IObjectState state);
}