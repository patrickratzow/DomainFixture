using System.Linq;
using DomainFixture.Generation;
using DomainFixture.Tests.Domain.ValueObjects;
using DomainFixture.Validation;

namespace DomainFixture.Tests.Generation;

public sealed class UsernameValidation : IFixtureValidator<Username>
{
    private readonly UsernameValidator _validator = new();

    public ValidationReport Validate(Username subject)
    {
        var result = _validator.Validate(subject);

        return new ValidationReport(result.Errors.Select(error =>
            new ValidationFailure(error.PropertyName, error.ErrorCode, error.ErrorMessage)));
    }
}

public sealed class UsernameFixture : IFixtureTestConfiguration<Username>
{
    public void Configure(IFixtureTestBuilder<Username> fixture)
    {
        fixture.Recipe("Validation")
            .Baseline(Baseline)
            .ValidateWith(Validator)
            .RulesFrom<UsernameValidator>();
    }

    public static Username Baseline() => Username.From("baseline");

    public static UsernameValidation Validator() => new();
}
