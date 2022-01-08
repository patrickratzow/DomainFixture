using FluentValidation;

namespace DomainFixture.Tests.Domain.ValueObjects;

public class Username : ValueOf<string, Username>
{
}

public class UsernameValidator : AbstractValidator<Username>
{
    public UsernameValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(128);
    }
}
