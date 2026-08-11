using FluentValidation;

namespace DomainFixture.Tests.Domain.ValueObjects;

public sealed class DomainResult<T>
{
    private DomainResult(bool isSuccess, T value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public T Value { get; }
    public string? Error { get; }

    public static DomainResult<T> Success(T value) => new(true, value, null);

    public static DomainResult<T> Failure(string error) => new(false, default!, error);
}

public sealed class ResultDisplayName
{
    private ResultDisplayName(string value)
    {
        Value = value;
    }

    public string Value { get; set; }

    public static DomainResult<ResultDisplayName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainResult<ResultDisplayName>.Failure("REQUIRED");
        if (value.Length > 24)
            return DomainResult<ResultDisplayName>.Failure("TOO_LONG");

        return DomainResult<ResultDisplayName>.Success(new ResultDisplayName(value));
    }
}

public sealed class ResultDisplayNameValidator : AbstractValidator<ResultDisplayName>
{
    public ResultDisplayNameValidator()
    {
        RuleFor(name => name.Value)
            .NotEmpty()
            .MaximumLength(24);
    }
}
