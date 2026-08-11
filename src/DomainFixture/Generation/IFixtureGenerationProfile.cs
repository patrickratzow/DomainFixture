using System;
using System.Linq.Expressions;

namespace DomainFixture.Generation;

/// <summary>
/// Declares assembly-wide defaults for generated fixture tests.
/// Source generators analyze this method; it is not executed at runtime.
/// </summary>
public interface IFixtureGenerationProfile
{
    void Configure(IFixtureGenerationOptions options);
}

public interface IFixtureGenerationOptions
{
    IFixtureConventionOptions Conventions();

    IFixtureActivationOptions Activation();

    IFixtureMutationOptions Mutations();

    [Obsolete("Install DomainFixture.Modules.FluentValidation in the validator project; module adapters are discovered automatically.")]
    IFixtureValidationOptions Validation();

    IFixtureOperationOptions Operations();

    IFixtureValueOptions Values();
}

/// <summary>
/// Supplies deterministic values when conventions cannot safely infer one. Expressions are
/// analyzed at compile time and are never invoked by DomainFixture.
/// </summary>
public interface IFixtureValueOptions
{
    IFixtureValueOptions For<TValue>(Expression<Func<TValue>> value);
}

public interface IFixtureOperationOptions
{
    IFixtureOperationOptions RejectWith<TException>()
        where TException : Exception;

    IFixtureOperationOptions RejectWith<TSubject, TException>()
        where TException : Exception;

    IFixtureOperationOptions UseResult<TSubject, TResult>(
        Expression<Func<TResult, bool>> isSuccess,
        Expression<Func<TResult, TSubject>> value);
}

[Obsolete("Install DomainFixture.Modules.FluentValidation in the validator project; module adapters are discovered automatically.")]
public interface IFixtureValidationOptions
{
    IFixtureValidationOptions UseFluentValidation();
}

public interface IFixtureMutationOptions
{
    IFixtureMutationOptions For<TSubject, TProperty>(
        Expression<Func<TSubject, TProperty>> property,
        Func<TSubject, TProperty, TSubject> reconstruction);
}

public interface IFixtureConventionOptions
{
    /// <summary>
    /// Synthesizes a valid baseline for every recipe that does not declare an explicit
    /// Baseline, Synthesize, or FromTransition source.
    /// </summary>
    IFixtureConventionOptions AutoSynthesizeRecipes();

    /// <summary>
    /// Creates implicit Valid recipes for constructible domain types reachable from an
    /// explicitly configured subject. Discovery follows typed properties and collection
    /// elements; it does not scan for unrelated application requests or services.
    /// </summary>
    IFixtureConventionOptions AutoDiscoverDomainTypes();

    IFixtureConventionOptions UseNullability();

    IFixtureConventionOptions UsePropertyNames();

    IFixtureConventionOptions UseImmutableObjects();

    IFixtureConventionOptions UseEntityIdentity();
}

/// <summary>
/// Selects how generated tests will activate runtime collaborators. Factory activation is
/// currently used; service-provider activation is reserved as an optional integration layer.
/// </summary>
public interface IFixtureActivationOptions
{
    IFixtureActivationOptions UseFactories();

    IFixtureActivationOptions UseServiceProvider<TFactory>()
        where TFactory : IFixtureServiceProviderFactory, new();
}

/// <summary>
/// Dependency-injection packages can adapt their container to this abstraction without
/// introducing a container dependency into DomainFixture.
/// </summary>
public interface IFixtureServiceProviderFactory
{
    IServiceProvider CreateServiceProvider();
}
