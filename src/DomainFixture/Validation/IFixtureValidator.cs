namespace DomainFixture.Validation;

/// <summary>
/// Adapts an application's validator to the result consumed by generated fixture tests.
/// Implementations construct their own dependencies and do not require a DI container.
/// </summary>
/// <typeparam name="T">The validated subject type.</typeparam>
public interface IFixtureValidator<in T>
{
    ValidationReport Validate(T subject);
}
