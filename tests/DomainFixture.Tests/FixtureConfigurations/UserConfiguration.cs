using System;
using DomainFixture.FixtureConfigurations;
using DomainFixture.Tests.Domain.Entities;
using DomainFixture.Tests.Domain.ValueObjects;

namespace DomainFixture.Tests.FixtureConfigurations;

public class UserConfiguration : IFixtureConfiguration<User>
{
    public void Configure(IFixtureConfigurationBuilder<User> builder)
    {
        builder.Property(x => x.Id)
            .Valid(Guid.NewGuid())
            .Invalid(Guid.Empty);

        builder.Property(x => x.Email)
            .Valid(EmailAddress.From("bak@sli.ng"))
            .Invalid(null!);

        builder.Property(x => x.Name)
            .Valid(Username.From("Baksling"))
            .Invalid(null!);

        builder.Property(x => x.CreatedAt)
            .Valid(DateTimeOffset.UtcNow.AddMinutes(-10))
            .Invalid(DateTimeOffset.UtcNow.AddMinutes(10));
        
        builder.Property(x => x.CreatedAt)
            .Valid(DateTimeOffset.UtcNow.AddMinutes(-10))
            .Invalid(DateTimeOffset.UtcNow.AddMinutes(10));
    }
}