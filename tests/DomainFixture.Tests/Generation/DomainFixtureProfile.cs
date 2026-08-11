using DomainFixture.Generation;
using DomainFixture.Tests.Domain.Entities;
using DomainFixture.Tests.Domain.ValueObjects;
using FluentValidation;

namespace DomainFixture.Tests.Generation;

public sealed class DomainFixtureProfile : IFixtureGenerationProfile
{
    public void Configure(IFixtureGenerationOptions options)
    {
        options.Conventions()
            .UseNullability()
            .UsePropertyNames()
            .UseImmutableObjects()
            .UseEntityIdentity();

        options.Operations()
            .RejectWith<Username, ValidationException>()
            .UseResult<ResultDisplayName, DomainResult<ResultDisplayName>>(
                result => result.IsSuccess,
                result => result.Value);

        options.Activation()
            .UseFactories();

        options.Values()
            .For<BillingCycle>(() => BillingCycle.Monthly);
    }
}
