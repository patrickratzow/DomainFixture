using DomainFixture.Tests.Domain.ValueObjects;

namespace DomainFixture.Tests.Domain.Entities;

public class User : BaseEntity
{
    public Username Name { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;

    private User()
    {
    }

    public static User From(Username name, EmailAddress email)
    {
        var instance = new User
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Name = name,
            Email = email
        };
        instance.Validate();

        return instance;
    }
}