using System.Linq;
using DomainFixture.Generation;
using DomainFixture.Tests.Domain.Entities;
using DomainFixture.Validation;

namespace DomainFixture.Tests.Generation;

public sealed class RegistrationRequestValidation : IFixtureValidator<RegistrationRequest>
{
    private readonly RegistrationRequestValidator _validator = new();

    public ValidationReport Validate(RegistrationRequest subject)
    {
        var result = _validator.Validate(subject);

        return new ValidationReport(result.Errors.Select(error =>
            new ValidationFailure(error.PropertyName, error.ErrorCode, error.ErrorMessage)));
    }
}

public sealed class RegistrationRequestFixture : IFixtureTestConfiguration<RegistrationRequest>
{
    public void Configure(IFixtureTestBuilder<RegistrationRequest> fixture)
    {
        fixture.Recipe("Registration")
            .Baseline(Baseline)
            .ValidateWith(Validator)
            .RulesFrom<RegistrationRequestValidator>();
    }

    public static RegistrationRequest Baseline() => new()
    {
        Description = "baseline"
    };

    public static RegistrationRequestValidation Validator() => new();
}
