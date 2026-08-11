using DomainFixture.Generation;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class OrderingShippingProfile : IFixtureGenerationProfile
{
    public void Configure(IFixtureGenerationOptions options)
    {
        options.Conventions()
            .AutoSynthesizeRecipes()
            .AutoDiscoverDomainTypes()
            .UseNullability()
            .UsePropertyNames()
            .UseImmutableObjects()
            .UseEntityIdentity();

        options.Activation()
            .UseFactories();
    }
}
