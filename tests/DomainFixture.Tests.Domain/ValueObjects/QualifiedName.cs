using FluentValidation;

namespace DomainFixture.Tests.Domain.ValueObjects;

public class QualifiedName
{
    public string Name { get; protected set; } = string.Empty;
    public string Realm { get; protected set; } = string.Empty;

    protected QualifiedName()
    {
    }

    public static QualifiedName From(string name, string realm) => new()
    {
        Name = name,
        Realm = realm
    };
}

public sealed class QualifiedNameValidator : AbstractValidator<QualifiedName>
{
    public QualifiedNameValidator()
    {
        RuleFor(value => value.Name)
            .NotEmpty()
            .MaximumLength(32);
        RuleFor(value => value.Realm)
            .NotEmpty()
            .MaximumLength(16);
    }
}
