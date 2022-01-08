namespace DomainFixture.Tests.Domain.Entities;

public abstract class BaseEntity : Validatable<BaseEntity>
{
    public Guid Id { get; protected init; }
    public DateTimeOffset CreatedAt { get; protected init; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
}

