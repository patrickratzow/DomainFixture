using FluentValidation;

namespace DomainFixture.Tests.Domain.ValueObjects;

public class EmailAddress : ValueOf<string, EmailAddress>
{
}

public class EmailAddressValidator : AbstractValidator<EmailAddress>
{
    public EmailAddressValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .EmailAddress();
    }
}