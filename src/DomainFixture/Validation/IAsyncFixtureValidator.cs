using System.Threading;
using System.Threading.Tasks;

namespace DomainFixture.Validation;

/// <summary>
/// Optional adapter for validators whose behavior is inherently asynchronous.
/// Synchronous validation should implement <see cref="IFixtureValidator{T}"/> instead.
/// </summary>
/// <typeparam name="T">The validated subject type.</typeparam>
public interface IAsyncFixtureValidator<in T>
{
    Task<ValidationReport> ValidateAsync(
        T subject,
        CancellationToken cancellationToken = default);
}
