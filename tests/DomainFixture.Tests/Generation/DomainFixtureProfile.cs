using DomainFixture.Generation;

namespace DomainFixture.Tests.Generation;

public sealed class DomainFixtureProfile : IFixtureGenerationProfile
{
    public void Configure(IFixtureGenerationOptions options)
    {
        options.Validation()
            .UseFluentValidation();

        options.Conventions()
            .UseNullability()
            .UsePropertyNames()
            .UseImmutableObjects();

        options.Activation()
            .UseFactories();

    }
}
