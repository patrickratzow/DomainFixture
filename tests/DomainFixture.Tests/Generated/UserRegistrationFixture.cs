using System.Linq;
using DomainFixture.Generation;
using DomainFixture.Validation;
using FluentValidation;

namespace DomainFixture.Tests.Generated;

public sealed class GeneratedUser
{
    public string Description { get; set; } = string.Empty;
}

public sealed class RegistrationUserValidator : AbstractValidator<GeneratedUser>
{
    public RegistrationUserValidator()
    {
        RuleFor(user => user.Description)
            .Length(4, 8)
            .WithErrorCode("DESCRIPTION_LENGTH");
    }
}

public sealed class RegistrationValidation : IFixtureValidator<GeneratedUser>
{
    private readonly RegistrationUserValidator _validator = new();

    public ValidationReport Validate(GeneratedUser subject)
    {
        var result = _validator.Validate(subject);

        return new ValidationReport(result.Errors.Select(error =>
            new ValidationFailure(error.PropertyName, error.ErrorCode, error.ErrorMessage)));
    }
}

public sealed class UserRegistrationFixture : IFixtureTestConfiguration<GeneratedUser>
{
    public void Configure(IFixtureTestBuilder<GeneratedUser> fixture)
    {
        fixture.Recipe("Registration")
            .Baseline(Baseline)
            .ValidateWith(Validator)
            .RulesFrom<RegistrationUserValidator>();
    }

    public static GeneratedUser Baseline() => new()
    {
        Description = "baseline"
    };

    public static RegistrationValidation Validator() => new();
}
