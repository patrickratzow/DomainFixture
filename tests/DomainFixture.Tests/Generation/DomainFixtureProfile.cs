using DomainFixture.Generation;
using DomainFixture.Tests.Domain.ValueObjects;

namespace DomainFixture.Tests.Generation;

public sealed class DomainFixtureProfile : IFixtureGenerationProfile
{
    public void Configure(IFixtureGenerationOptions options)
    {
        options.Validation()
            .UseFluentValidation();

        options.Conventions()
            .UseNullability()
            .UsePropertyNames();

        options.Activation()
            .UseFactories();

        options.Mutations()
            .For<Username, string>(
                username => username.Value,
                UsernameTestFactory.WithValue);
    }
}

public static class UsernameTestFactory
{
    public static Username WithValue(Username _, string value) =>
        new UncheckedUsername(value);

    private sealed class UncheckedUsername : Username
    {
        public UncheckedUsername(string value)
        {
            Value = value;
        }
    }
}
