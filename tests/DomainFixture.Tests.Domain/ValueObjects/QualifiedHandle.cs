using FluentValidation;

namespace DomainFixture.Tests.Domain.ValueObjects;

public sealed record QualifiedHandle(string Name, string Realm, int Level);

public sealed class QualifiedHandleValidator : AbstractValidator<QualifiedHandle>
{
    public QualifiedHandleValidator()
    {
        RuleFor(value => value.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(32);
        RuleFor(value => value.Realm)
            .NotEmpty()
            .MaximumLength(16);
        RuleFor(value => value.Level)
            .InclusiveBetween(1, 10);
    }
}
