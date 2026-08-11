using FluentValidation;

namespace DomainFixture.Tests.Domain.Entities;

public sealed class RegistrationRequest
{
    public string Description { get; set; } = string.Empty;
}

public sealed class RegistrationRequestValidator : AbstractValidator<RegistrationRequest>
{
    public RegistrationRequestValidator()
    {
        RuleFor(request => request.Description)
            .NotNull()
            .WithErrorCode("DESCRIPTION_REQUIRED")
            .NotEmpty()
            .WithErrorCode("DESCRIPTION_NOT_EMPTY")
            .Length(4, 8)
            .WithErrorCode("DESCRIPTION_LENGTH");
    }
}
