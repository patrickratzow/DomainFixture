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

    IFixtureValidationOptions Validation();
}

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
    IFixtureConventionOptions UseNullability();

    IFixtureConventionOptions UsePropertyNames();

    IFixtureConventionOptions UseImmutableObjects();
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
