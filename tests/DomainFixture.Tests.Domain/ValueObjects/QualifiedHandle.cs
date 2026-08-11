using FluentValidation;

namespace DomainFixture.Tests.Domain.ValueObjects;

public sealed record QualifiedHandle(string Name, string Realm);

public sealed class QualifiedHandleValidator : AbstractValidator<QualifiedHandle>
{
    public QualifiedHandleValidator()
    {
        RuleFor(value => value.Name)
            .NotEmpty()
            .MaximumLength(32);
        RuleFor(value => value.Realm)
            .NotEmpty()
            .MaximumLength(16);
    }
}
